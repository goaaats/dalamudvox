using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Dalamud.Game.ClientState;
using Dalamud.Plugin;
using DalamudPluginProjectTemplate;
using DalamudPluginProjectTemplate.Attributes;
using NAudio.Wave;

namespace DalamudVox
{
    public class Plugin : IDalamudPlugin
    {
        private DalamudPluginInterface pluginInterface;

        private PluginCommandManager<Plugin> commandManager;
        public Configuration Config;
        private PluginUI ui;

        private CultureInfo definedCulture = CultureInfo.GetCultureInfo("en-US");

        private GameFunctions funcs;

        private bool isListening = true;

        private bool keysDown = false;

        public string Name => "DalamudVox";

        private Dictionary<string, (string, GrammarBuilder)> customActions = new Dictionary<string, (string, GrammarBuilder)>();

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;

            Config = (Configuration) this.pluginInterface.GetPluginConfig() ?? new Configuration();
            Config.Initialize(this.pluginInterface);

            ui = new PluginUI(this);
            this.pluginInterface.UiBuilder.OnBuildUi += ui.Draw;
            this.pluginInterface.UiBuilder.OnBuildUi += OnNewFrame;

            this.commandManager = new PluginCommandManager<Plugin>(this, this.pluginInterface);

            funcs = new GameFunctions(pluginInterface.TargetModuleScanner);

            try
            {
                Thread.CurrentThread.CurrentCulture = definedCulture;
                SetupSpeech();
                this.pluginInterface.Framework.Gui.Chat.Print("Voice recognition OK!");

                this.pluginInterface.SubscribeAny((pName, obj) =>
                {
                    dynamic message = obj;

                    if ((string) message.Task == "RegisterAction")
                    {
                        customActions.Add((string) message.ActionName, ((string) message.Action, (GrammarBuilder) message.ParameterGrammar));
                    }

                    if ((string) message.Task == "UnregisterAction")
                    {
                        customActions.Remove((string) message.ActionName);
                    }

                    SetupSpeech();
                });

                dynamic message = new ExpandoObject();
                message.Reason = "Loaded";

                this.pluginInterface.SendMessage(message);
            }
            catch (Exception ex)
            {
                PluginLog.LogError(ex, "Could not init voice recognition");
                this.pluginInterface.Framework.Gui.Chat.PrintError("Could not start \"Hey, Dalamud!\".\nPlease make sure that you have the American English Windows Language Pack installed.");
            }
        }

        private void OnNewFrame()
        {
            // We don't want to open the UI before the player loads, that leaves the options uninitialized.
            if (this.pluginInterface.ClientState.LocalPlayer == null) return;

            if (this.pluginInterface.ClientState.KeyState[(byte)this.Config.ModifierKey] &&
                this.pluginInterface.ClientState.KeyState[(byte)this.Config.MajorKey])
            {
                if (this.keysDown) return;

                PluginLog.Log("Manual HD trigger, state: {0} keysDown: {1}", state, keysDown);

                if (this.state == DetectionState.Action) return;

                this.keysDown = true;
                
                SwitchToActionMode();

                return;
            }

            this.keysDown = false;
        }

        private Grammar heyDalamudGrammar;

        private Grammar actionGrammar;

        private SpeechRecognitionEngine recognizer;
        private SpeechSynthesizer synthesizer = new SpeechSynthesizer();

        private DetectionState state;

        private readonly CancellationTokenSource noComprehendTaskCancellationTokenSource = new CancellationTokenSource();

        public enum DetectionState
        {
            Hotword,
            Action
        }

        [DllImport("winmm.dll")]
        public static extern int waveInGetNumDevs();

        private GrammarBuilder[] ActionArguments =
        {
            new GrammarBuilder(new Choices("Gridania", "New Gridania")),
            "Bentbranch Meadows",
            new GrammarBuilder(new Choices("Hawthorne Hut", "The Hawthorne Hut")),
            "Quarrymill",
            "Camp Tranquil",
            "Fallgourd Float",
            new GrammarBuilder(new Choices("Limsa Lominsa", "Limsa Lominsa Aetheryte Plaza")),
            "Ul'dah",
            "Moraby Drydocks",
            "Costa del Sol",
            "Wineport",
            "Swiftperch",
            "Aleport",
            "Camp Bronze Lake",
            "Horizon",
            "Camp Drybone",
            "Little Ala Mhigo",
            "Forgotten Springs",
            "Camp Bluefog",
            "Camp Dragonhead",
            "Revenant's Toll",
            "Summerford Farms",
            "Black Brush Station",
            new GrammarBuilder(new Choices("Wolves' Den", "Wolves' Den Pier")),
            new GrammarBuilder(new Choices("Gold Saucer", "The Gold Saucer")),
            new GrammarBuilder(new Choices("Foundation", "The Foundation")),
            new GrammarBuilder(new Choices("Falcon's Nest", "The Falcon's Nest")),
            "Camp Cloudtop",
            "Helix",
            "Idyllshire",
            "Tailfeather",
            "Anyx Trine",
            "Moghome",
            "Zenith",
            "Castrum Oriens",
            "The Peering Stones",
            "Ala Ghanna",
            "Ala Ghiri",
            "The Ala Mhigan Quarter",
            "Rhalgr's Reach",
            "Tamamizu",
            "Onokoro",
            "Namai",
            new GrammarBuilder(new Choices("House of the Fierce", "The House of the Fierce")),
            "Reunion",
            new GrammarBuilder(new Choices("Dawn Throne", "The Dawn Throne")),
            "Kugane",
            new GrammarBuilder(new Choices("Doman Enclave", "The Doman Enclave")),
            "Fort Job",
            new GrammarBuilder(new Choices("Crystarium", "The Crystarium")),
            "Eulmore",
            "Stilltide",
            "Wright",
            "Tomra",
            "Twine",
            "Slitherbough",
            "Fanow",
            new GrammarBuilder(new Choices("Ondo Cups", "The Ondo Cups")),
            new GrammarBuilder(new Choices("Macarenses Angle", "The Macarenses Angle")),
            "the Inn at journey's head",
            new GrammarBuilder(new Choices("Company", "Free Company")),
            new GrammarBuilder(new Choices("Private", "Estate Hall")),
            "one",
            "two",
            "three",
            "four",
            "five",
            "six",
            "seven",
            "eight",
            "nine",
            "ten",
            "eleven",
            "twelve",
            "thirteen",
            "fourteen",
            "fifteen",
            "sixteen",
            "seventeen",
            "eighteen",
            "nineteen",
            "twenty",
            "twentyone",
            "twentytwo",
            "twentythree",
            "twentyfour",
            "twentyfive",
            "twentysix",
            "twentyseven",
            "twentyeight",
            "twentynine",
            "thirty",
            "thirtyone",
            "thirtytwo",
            "thirtythree",
            "thirtyfour",
            "thirtyfive",
            "thirtysix",
            "thirtyseven",
            "thirtyeight",
            "thirtynine",
            "forty",
            "fortyone",
            "fortytwo",
            "fortythree",
            "fortyfour",
            "fortyfive"
        };

        private WaveOut _waveOutPlayer;
        private AudioFileReader _reader;

        private void SetupSpeech()
        {
            state = DetectionState.Hotword;

            recognizer?.RecognizeAsyncStop();
            recognizer?.Dispose();

            _waveOutPlayer = new WaveOut();
            _reader = new AudioFileReader(Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(typeof(Plugin)).Location),
                "sounds", "sound.wav"));

            _waveOutPlayer.Init(_reader);
            _waveOutPlayer.Volume = Config.Volume / 100.0f;

            synthesizer.SetOutputToDefaultAudioDevice();

            recognizer = new SpeechRecognitionEngine(definedCulture);

            var numDevs = waveInGetNumDevs();
            PluginLog.Log("[REC] NumDevs: {0}", numDevs);

            var heyDalamudBuilder = new GrammarBuilder("hey dalamud");
            heyDalamudBuilder.Culture = definedCulture;

            heyDalamudGrammar = new Grammar(heyDalamudBuilder);
            heyDalamudGrammar.Name = "heyDalamudGrammar";

            var actionBuilder = new GrammarBuilder();
            actionBuilder.Culture = definedCulture;

            var choicePhrases = new List<string>()
            {
                "teleport to", "open macros", "open item search",
                "open the plugin installer", "help me", "pray return to the waking sands", "equip gearset",
                "open macros", "open inventory"
            };

            choicePhrases.AddRange(customActions.Select(x => x.Value.Item1));

            actionBuilder.Append(new Choices(choicePhrases.ToArray()));

            var actionArgumentList = new List<GrammarBuilder>();
            actionArgumentList.AddRange(customActions.Select(x => x.Value.Item2));

            actionBuilder.Append(new GrammarBuilder(new GrammarBuilder(new Choices(actionArgumentList.ToArray())), 0, 1));

            actionGrammar = new Grammar(actionBuilder)
            {
                Name = "actionGrammar"
            };

            // Create and load a dictation grammar.
            recognizer.LoadGrammar(heyDalamudGrammar);

            // Add a handler for the speech recognized event.  
            recognizer.SpeechRecognized +=
                recognizer_SpeechRecognized;

            // Configure input to the speech recognizer.  
            recognizer.SetInputToDefaultAudioDevice();

            // Start asynchronous, continuous speech recognition.  
            recognizer.RecognizeAsync(RecognizeMode.Multiple);
        }

        private void SwitchToActionMode()
        {
            PluginLog.Log("SwitchToActionMode");

            recognizer.RecognizeAsyncStop();

            recognizer.UnloadAllGrammars();
            recognizer.LoadGrammar(actionGrammar);

            recognizer.SetInputToDefaultAudioDevice();
            recognizer.RecognizeAsync(RecognizeMode.Single);

            
            if (_waveOutPlayer.PlaybackState == PlaybackState.Playing)
                _waveOutPlayer.Stop();

            _waveOutPlayer.Init(_reader);
            _waveOutPlayer.Play();

            state = DetectionState.Action;

            Task.Run(() =>
            {
                PluginLog.Log("No comprehend task start");

                Thread.Sleep(9000);

                if (this.state != DetectionState.Action)
                    return;

                PluginLog.Log("Comprehension failed");

                synthesizer.SpeakAsync("Sorry, I didn't quite get that, please try again.");
                recognizer.RecognizeAsyncStop();

                recognizer.UnloadAllGrammars();
                recognizer.LoadGrammar(heyDalamudGrammar);

                state = DetectionState.Hotword;
                recognizer.SetInputToDefaultAudioDevice();
                recognizer.RecognizeAsync(RecognizeMode.Multiple);
            }, noComprehendTaskCancellationTokenSource.Token);
        }

        private void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            PluginLog.Log("[REC] In mode: {0} Recognized text: {1}", state, e.Result.Text);

            synthesizer.Volume = Config.Volume;

            try
            {
                switch (state)
                {
                    case DetectionState.Hotword:

                        if (Config.DisableInInstance && (this.pluginInterface.ClientState.Condition[ConditionFlag.BoundByDuty] ||
                                                         this.pluginInterface.ClientState.Condition[ConditionFlag.BoundByDuty56] ||
                                                         this.pluginInterface.ClientState.Condition[ConditionFlag.BoundByDuty95] ||
                                                         this.pluginInterface.ClientState.Condition[ConditionFlag.BoundToDuty97] ||
                                                         this.pluginInterface.ClientState.Condition[ConditionFlag.InDeepDungeon]))
                            return;

                        if (!isListening)
                            return;

                        SwitchToActionMode();

                        break;

                    case DetectionState.Action:
                        state = DetectionState.Hotword;

                        dynamic message = new ExpandoObject();
                        message.Reason = "Recognized"; 
                        message.RecognizedAction = e.Result.Text;

                        this.pluginInterface.SendMessage(message);

                        noComprehendTaskCancellationTokenSource.Cancel();

                        recognizer.RecognizeAsyncStop();


                        var firstName = pluginInterface.ClientState.LocalPlayer != null
                            ? pluginInterface.ClientState.LocalPlayer.Name.Split()[0]
                            : string.Empty;

                        // realistic delay for interpretation
                        Thread.Sleep(700);

                        if (e.Result.Text.StartsWith("teleport to"))
                        {
                            var tpLoc = e.Result.Text.Substring("teleport to".Length);

                            if (string.IsNullOrEmpty(tpLoc))
                            {
                                synthesizer.Speak("You didn't tell me where to teleport to. Please try again.");
                            }
                            else
                            {
                                synthesizer.SpeakAsync($"Ok {firstName}, teleporting to {tpLoc}");

                                pluginInterface.CommandManager.ProcessCommand("/tp " + tpLoc);
                            }
                        }
                        else if (e.Result.Text.StartsWith("open item search"))
                        {
                            synthesizer.SpeakAsync($"Ok {firstName}, opening the item search");

                            Thread.Sleep(100);

                            pluginInterface.CommandManager.ProcessCommand("/xlitem");
                        }
                        else if (e.Result.Text.StartsWith("open the plugin installer"))
                        {
                            synthesizer.SpeakAsync($"Ok {firstName}, opening the plugin installer");

                            Thread.Sleep(100);

                            pluginInterface.CommandManager.ProcessCommand("/xlplugins");
                        }
                        else if (e.Result.Text.StartsWith("help me"))
                        {
                            pluginInterface.Framework.Gui.SetBgm(326);
                            synthesizer.Speak(
                                $"Hello {firstName}, thank you kindly for installing this plugin. Welcome to this virtual tour, which we will experience together! I am Dalamud, your virtual, intelligent FFXIV assistant, developed for you by our best R&D personnel. You can access me any time you are logged in by saying hey dalamud. Isn't that amazing? I am now opening a site in your browser that shows all commands I can execute for you. Now go and have fun! If you encounter any issues, feel free to post in the discord server. You are welcome.");
                            Thread.Sleep(200);
                            Process.Start("https://github.com/goaaats/dalamudvox/wiki/Commands");
                            pluginInterface.Framework.Gui.SetBgm(999);
                        }
                        else if (e.Result.Text.StartsWith("pray return to the waking sands"))
                        {
                            synthesizer.SpeakAsync("Tis an honor.");

                            Thread.Sleep(200);

                            pluginInterface.CommandManager.ProcessCommand("/tp Horizon");
                        }
                        else if (e.Result.Text.StartsWith("equip gearset"))
                        {
                            var gsWord = e.Result.Text.Substring("equip gearset ".Length);
                            var gsNum = WordToNumber(gsWord);

                            if (gsNum == -1)
                            {
                                synthesizer.Speak("You didn't tell me which gearset to equip. Please try again.");
                            }
                            else
                            {
                                synthesizer.SpeakAsync($"Ok {firstName}, equipping gearset {gsWord}");

                                funcs.ProcessChatBox("/gearset equip " + gsNum);
                            }
                        }
                        else if (e.Result.Text.StartsWith("open inventory"))
                        {
                            synthesizer.SpeakAsync($"Ok {firstName}, opening the inventory");

                            Thread.Sleep(100);

                            funcs.ProcessChatBox("/inventory");
                        }
                        else if (e.Result.Text.StartsWith("open macros"))
                        {
                            synthesizer.SpeakAsync($"Ok {firstName}, opening the plugin installer");

                            Thread.Sleep(100);

                            funcs.ProcessChatBox("/macros");
                        }

                        recognizer.UnloadGrammar(actionGrammar);
                        recognizer.LoadGrammar(heyDalamudGrammar);

                        recognizer.SetInputToDefaultAudioDevice();
                        recognizer.RecognizeAsync(RecognizeMode.Multiple);
                        break;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "Error in voice handling.");
                this.pluginInterface.Framework.Gui.Chat.PrintError("Could not handle your voice input. Please report this error.");
            }
        }

        [Command("/hdtoggle")]
        [HelpMessage("Toggle the \"Hey, Dalamud!\" speech recognition.")]
        public void ToggleSpeechCommand(string command, string args)
        {
            // You may want to assign these references to private variables for convenience.
            // Keep in mind that the local player does not exist until after logging in.
            var chat = this.pluginInterface.Framework.Gui.Chat;

            isListening = !isListening;

            chat.Print(isListening ? "Now listening." : "Not listening to you anymore.");
        }

        [Command("/hdconfig")]
        [HelpMessage("Open the \"Hey, Dalamud!\" configuration window.")]
        public void OpenConfigCommand(string command, string args)
        {
            ui.IsVisible = true;
        }

        private static int WordToNumber(string word) => word switch
        {
            "one" => 1,
            "two" => 2,
            "three" => 3,
            "four" => 4,
            "five" => 5,
            "six" => 6,
            "seven" => 7,
            "eight" => 8,
            "nine" => 9,
            "ten" => 10,
            "eleven" => 11,
            "twelve" => 12,
            "thirteen" => 13,
            "fourteen" => 14,
            "fifteen" => 15,
            "sixteen" => 16,
            "seventeen" => 17,
            "eighteen" => 18,
            "nineteen" => 19,
            "twenty" => 20,
            "twentyone" => 21,
            "twentytwo" => 22,
            "twentythree" => 23,
            "twentyfour" => 24,
            "twentyfive" => 25,
            "twentysix" => 26,
            "twentyseven" => 27,
            "twentyeight" => 28,
            "twentynine" => 29,
            "thirty" => 30,
            "thirtyone" => 31,
            "thirtytwo" => 32,
            "thirtythree" => 33,
            "thirtyfour" => 34,
            "thirtyfive" => 35,
            "thirtysix" => 36,
            "thirtyseven" => 37,
            "thirtyeight" => 38,
            "thirtynine" => 39,
            "forty" => 40,
            "fortyone" => 41,
            "fortytwo" => 42,
            "fortythree" => 43,
            "fortyfour" => 44,
            "fortyfive" => 45,
            _ => -1
        };

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            this.commandManager.Dispose();

            this.pluginInterface.UnsubscribeAny();

            _waveOutPlayer.Stop();
            _waveOutPlayer.Dispose();
            _reader.Dispose();

            synthesizer.Dispose();
            recognizer.RecognizeAsyncStop();
            recognizer.Dispose();

            pluginInterface.SavePluginConfig(Config);

            pluginInterface.UiBuilder.OnBuildUi -= ui.Draw;

            pluginInterface.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}