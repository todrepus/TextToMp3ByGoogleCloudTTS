using Google.Cloud.TextToSpeech.V1;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;

namespace TextToMp3.Model {
    class TTS {
        TextToSpeechClient client = null;
        
        public bool IsAvailable { get { return client != null; }}
        public void Init(string jsonPath) {
            try {
                TextToSpeechClientBuilder builder = new TextToSpeechClientBuilder {
                    CredentialsPath = jsonPath
                };
                client = builder.Build();
            } catch {
                throw new Exception("Client Build Error");
            }
        }  
        public async Task<ListVoicesResponse> GetVoiceList() {
            var response = await client.ListVoicesAsync(new ListVoicesRequest());
            
            return response;
        }


        public async Task UpdateVoiceOnVoiceStorage() {
            VoiceStorage storage = VoiceStorage.Instance;
            var response = await GetVoiceList();
            foreach (Voice voice in response.Voices) {
                var linqResult =
                    from idLevel in storage.idRoots
                    where voice.LanguageCodes.Contains(idLevel.Id)
                    from countryLevel in idLevel.countryLevels
                    from voiceTypeLevel in countryLevel.typeLevels
                    from voiceNameLevel in voiceTypeLevel.voiceNameSexs
                    where voiceNameLevel.voiceName == voice.Name
                    select voiceNameLevel;
                
                foreach(var voiceNameLevel in linqResult) {
                    voiceNameLevel.voice = voice;
                }
                
            }
            Console.WriteLine("");
        }
        public bool SynthesizeText(string text, string langCode, Voice voice, double speed, string soundFile="output.mp3") {
            try {

                using (FileStream fileStream = new FileStream(soundFile, FileMode.Create, FileAccess.Write)) {
                    var response = client.SynthesizeSpeech(new SynthesizeSpeechRequest {
                        Input = new SynthesisInput {
                            Text = text
                        },
                        // Note: voices can also be specified by name
                        Voice = new VoiceSelectionParams {
                            LanguageCode = langCode,
                            Name = voice.Name,
                            SsmlGender = voice.SsmlGender,
                        },
                        AudioConfig = new AudioConfig {
                            AudioEncoding = AudioEncoding.Mp3,
                            SampleRateHertz = voice.NaturalSampleRateHertz,
                            SpeakingRate = speed,
                            Pitch = 0
                        }
                    });


                    response.AudioContent.WriteTo(fileStream);
                }
            } catch {
                return false;
            } 
            return true;


        }

        public void TransformText(string text, string langCode, Voice voice, double speed, string soundFile="output.mp3") {

            text = String.Format("<speak>{0}<break time=\"1s\"/></speak>", text);

            var response = client.SynthesizeSpeech(new SynthesizeSpeechRequest {
                Input = new SynthesisInput {
                    Ssml = text
                },
                // Note: voices can also be specified by name
                Voice = new VoiceSelectionParams {
                    LanguageCode = langCode,
                    Name = voice.Name,
                    SsmlGender = voice.SsmlGender,
                },
                AudioConfig = new AudioConfig {
                    AudioEncoding = AudioEncoding.Mp3,
                    SampleRateHertz = voice.NaturalSampleRateHertz,
                    SpeakingRate = speed
                }
            });

            using (Stream output = File.Create(soundFile)) {
                response.AudioContent.WriteTo(output);
            }
        }
    }
}
