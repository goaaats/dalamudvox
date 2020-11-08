using System;
using System.IO;
using System.Media;
using System.Reflection;
using System.Speech.Recognition;
using Dalamud.Plugin;
using DalamudPluginProjectTemplate.Attributes;

namespace DalamudPluginProjectTemplate
{
    public class Plugin : IDalamudPlugin
    {
        private DalamudPluginInterface pluginInterface;
        private PluginCommandManager<Plugin> commandManager;
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

            this.commandManager = new PluginCommandManager<Plugin>(this, this.pluginInterface);
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

        private void SetupSpeechRecognition()
        {
            var heyDalamudBuilder = new GrammarBuilder("hey dalamud");

            heyDalamudGrammar = new Grammar(heyDalamudBuilder);
            heyDalamudGrammar.Name = "heyDalamudGrammar";

            var actionBuilder = new GrammarBuilder();

            actionBuilder.Append(new Choices(new[] { "teleport to", "open macros" }));

            actionBuilder.Append(new Choices(new[] { "limsa lominsa", "ahm araeng", "crystarium" }));

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
            recognizer.RecognizeAsync(RecognizeMode.Single);
        }

        static void recognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Console.WriteLine("Recognized text: " + e.Result.Text);

            switch (state)
            {
                case DetectionState.Hotword:

                    recognizer.RecognizeAsyncStop();

                    recognizer.UnloadGrammar(heyDalamudGrammar);
                    recognizer.LoadGrammar(actionGrammar);

                    recognizer.RecognizeAsync(RecognizeMode.Single);

                    player.SoundLocation = Path.Combine(new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName, "sound.wav");
                    player.Play();

                    state = DetectionState.Verb;
                    break;

                case DetectionState.Verb:
                    recognizer.RecognizeAsyncStop();

                    recognizer.UnloadGrammar(actionGrammar);
                    recognizer.LoadGrammar(heyDalamudGrammar);

                    recognizer.RecognizeAsync(RecognizeMode.Single);

                    state = DetectionState.Hotword;
                    break;
            }


        }

        [Command("/startrec")]
        [HelpMessage("Example help message.")]
        public void StartRecCommand(string command, string args)
        {
            SetupSpeechRecognition();
            this.pluginInterface.Framework.Gui.Chat.Print($"OK!");
        }

        #region IDisposable Support
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            this.commandManager.Dispose();

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
