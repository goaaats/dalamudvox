using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin;
using DalamudPluginProjectTemplate.Attributes;
using Lumina.Excel.GeneratedSheets;

namespace DalamudPluginProjectTemplate
{
    public class Plugin : IDalamudPlugin
    {
        private DalamudPluginInterface pluginInterface;
        //private PluginCommandManager<Plugin> commandManager;
        private Configuration config;
        private PluginUI ui;

        public string Name => "DalamudVox";

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;

            this.config = (Configuration)this.pluginInterface.GetPluginConfig() ?? new Configuration();
            this.config.Initialize(this.pluginInterface);

            this.ui = new PluginUI();
            this.pluginInterface.UiBuilder.OnBuildUi += this.ui.Draw;

            //this.commandManager = new PluginCommandManager<Plugin>(this, this.pluginInterface);

            SetupSpeechRecognition();
            this.pluginInterface.Framework.Gui.Chat.Print("Voice recognition OK!");
        }

        private static Grammar heyDalamudGrammar;

        private static Grammar actionGrammar;

        private static SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine(
            new System.Globalization.CultureInfo("en-US"));

        private static DetectionState state;

        public enum DetectionState
        {
            Hotword,
            Verb
        }

        private static System.Media.SoundPlayer player = new SoundPlayer();

        [DllImport("winmm.dll")]
        public static extern int waveInGetNumDevs();

        public static GrammarBuilder[] AllowedAetheryte = new GrammarBuilder[]
        {
            new GrammarBuilder(new Choices(new []{"Gridania", "New Gridania"})), 
            "Bentbranch Meadows",
            new GrammarBuilder(new Choices(new []{"Hawthorne Hut", "The Hawthorne Hut"})), 
            "Quarrymill",
            "Camp Tranquil",
            "Fallgourd Float",
            new GrammarBuilder(new Choices(new []{"Limsa Lominsa", "Limsa Lominsa Aetheryte Plaza"})), 
            "Uldah",
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
            "Revenants Toll",
            "Summerford Farms",
            "Black Brush Station",
            new GrammarBuilder(new Choices(new []{"Wolves' Den", "Wolves' Den Pier"})), 
            new GrammarBuilder(new Choices(new []{"Gold Saucer", "The Gold Saucer"})),
            new GrammarBuilder(new Choices(new []{"Foundation", "The Foundation"})),
            new GrammarBuilder(new Choices(new []{"Falcon's Nest", "The Falcon's Nest"})),
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
            new GrammarBuilder(new Choices(new []{"House of the Fierce", "The House of the Fierce"})),
            "Reunion",
            new GrammarBuilder(new Choices(new []{"Dawn Throne", "The Dawn Throne"})),
            "Kugane",
            new GrammarBuilder(new Choices(new []{"Doman Enclave", "The Doman Enclave"})),
            "Fort Job",
            new GrammarBuilder(new Choices(new []{"Crystarium", "The Crystarium"})),
            "Eulmore",
            "Stilltide",
            "Wright",
            "Tomra",
            "Twine",
            "Slitherbough",
            "Fanow",
            new GrammarBuilder(new Choices(new []{"Ondo Cups", "The Ondo Cups"})),
            new GrammarBuilder(new Choices(new []{"Macarenses Angle", "The Macarenses Angle"})),
            "the Inn at journey's head",
        };

        private void SetupSpeechRecognition()
        {
            var numDevs = waveInGetNumDevs();
            PluginLog.Log("[REC] NumDevs: {0}", numDevs);

            var heyDalamudBuilder = new GrammarBuilder("hey dalamud");

            heyDalamudGrammar = new Grammar(heyDalamudBuilder);
            heyDalamudGrammar.Name = "heyDalamudGrammar";

            var actionBuilder = new GrammarBuilder();

            actionBuilder.Append(new Choices(new[] { "teleport to", "open macros", "open item search", "open the plugin installer", "help me" }));

            actionBuilder.Append(new GrammarBuilder(new GrammarBuilder(new Choices(AllowedAetheryte)), 0, 1));

            actionGrammar = new Grammar(actionBuilder);
            actionGrammar.Name = "actionGrammar";

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

        void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            PluginLog.Log("Recognized text: " + e.Result.Text);

            var synthesizer = new SpeechSynthesizer();
            synthesizer.SetOutputToDefaultAudioDevice();

            try
            {
                switch (state)
                {
                    case DetectionState.Hotword:

                        recognizer.RecognizeAsyncStop();

                        recognizer.UnloadGrammar(heyDalamudGrammar);
                        recognizer.LoadGrammar(actionGrammar);

                        recognizer.SetInputToDefaultAudioDevice();
                        recognizer.RecognizeAsync(RecognizeMode.Single);

                        player.SoundLocation =
                            Path.Combine(Path.GetDirectoryName(Assembly.GetAssembly(typeof(Plugin)).Location),
                                "sound.wav");
                        player.Play();

                        state = DetectionState.Verb;

                        Task.Run(() =>
                        {
                            Thread.Sleep(9000);


                            if (state != DetectionState.Verb) 
                                return;


                            synthesizer.SpeakAsync("Sorry, I didn't quite get that, please try again.");
                            recognizer.RecognizeAsyncStop();

                            recognizer.UnloadAllGrammars();
                            recognizer.LoadGrammar(heyDalamudGrammar);

                            state = DetectionState.Hotword;
                            recognizer.SetInputToDefaultAudioDevice();
                            recognizer.RecognizeAsync(RecognizeMode.Multiple);

                        });

                        break;

                    case DetectionState.Verb:
                        state = DetectionState.Hotword;

                        recognizer.RecognizeAsyncStop();


                        var firstName = this.pluginInterface.ClientState.LocalPlayer != null
                            ? this.pluginInterface.ClientState.LocalPlayer.Name.Split()[0]
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
                                synthesizer.Speak($"Ok {firstName}, teleporting to {tpLoc}");

                                Thread.Sleep(200);

                                this.pluginInterface.CommandManager.ProcessCommand("/tp " + tpLoc); 
                            }
                        } 
                        else if (e.Result.Text.StartsWith("open item search"))
                        {
                            synthesizer.Speak($"Ok {firstName}, opening the item search");

                            Thread.Sleep(100);

                            this.pluginInterface.CommandManager.ProcessCommand("/xlitem");
                        }
                        else if (e.Result.Text.StartsWith("open the plugin installer"))
                        {
                            synthesizer.Speak($"Ok {firstName}, opening the plugin installer");

                            Thread.Sleep(100);

                            this.pluginInterface.CommandManager.ProcessCommand("/xlplugins");
                        }
                        else if (e.Result.Text.StartsWith("help me"))
                        {
                            synthesizer.Speak($"Hello {firstName}, thank you kindly for installing this plugin. Welcome to this virtual tour, which we will experience together! I am Dalamud, your virtual, intelligent FFXIV assistant, developed for you by our best R&D personnel. You can access me any time you are logged in by saying hey dalamud. Currently, you can say teleport to to teleport to any location in-game. You can also open the plugin installer and the item search just with your vocal chords. Isn't that amazing? Now go and have fun!");

                            Thread.Sleep(100);

                            this.pluginInterface.CommandManager.ProcessCommand("/xlplugins");
                        }

                        recognizer.UnloadGrammar(actionGrammar);
                        recognizer.LoadGrammar(heyDalamudGrammar);

                        recognizer.SetInputToDefaultAudioDevice();
                        recognizer.RecognizeAsync(RecognizeMode.Multiple);
                        break;
                }
            }
            catch(Exception ex)
            {
                PluginLog.Error(ex, "Error in voice handling.");
            }
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            //this.commandManager.Dispose();

            this.pluginInterface.SavePluginConfig(this.config);

            this.pluginInterface.UiBuilder.OnBuildUi -= this.ui.Draw;

            this.pluginInterface.Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
