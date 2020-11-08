using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;

namespace Tester
{
    class Program
    {
        private static Grammar heyDalamudGrammar;

        private static Grammar actionGrammar;

        private static SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine(
            new System.Globalization.CultureInfo("en-US"));

        private static DetectionState state;

        private static System.Media.SoundPlayer player = new SoundPlayer();

        public enum DetectionState
        {
            Hotword,
            Verb
        }

        static void Main(string[] args)
        {

            var heyDalamudBuilder = new GrammarBuilder("hey dalamud");

            heyDalamudGrammar = new Grammar(heyDalamudBuilder);
            heyDalamudGrammar.Name = "heyDalamudGrammar";

            var actionBuilder = new GrammarBuilder();

            actionBuilder.Append(new Choices(new []{"teleport to", "open macros"}));

            actionBuilder.Append(new Choices(new[] { "limsa lominsa", "ahm araeng", "crystarium" }));

            actionGrammar = new Grammar(actionBuilder);
            actionGrammar.Name = "actionGrammar";

            // Create and load a dictation grammar.
            recognizer.LoadGrammar(heyDalamudGrammar);

            // Add a handler for the speech recognized event.  
            recognizer.SpeechRecognized +=
                new EventHandler<SpeechRecognizedEventArgs>(recognizer_SpeechRecognized);

            // Configure input to the speech recognizer.  
            recognizer.SetInputToDefaultAudioDevice();

            // Start asynchronous, continuous speech recognition.  
            recognizer.RecognizeAsync(RecognizeMode.Single);

            // Keep the console window open.  
            while (true)
            {
                Console.ReadLine();
            }
        }

        // Handle the SpeechRecognized event.  
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

                    player.SoundLocation = "sound.wav";
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
    }
}
