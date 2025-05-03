using NAudio.Wave;
using NAudio.MediaFoundation;
using DotNetEnv;

class Program
{
    static void Main(string[] args)
    {
        Env.Load();

        string? groqApiKey =  Environment.GetEnvironmentVariable("GROQ_API_KEY"); // Load the API key from environment variable

        Console.WriteLine("Capturing loopback audio (what you hear)...");
        string outputFilePath = "loopback_capture.wav";
        using (var capture = new WasapiLoopbackCapture())
        using (var writer = new WaveFileWriter(outputFilePath, capture.WaveFormat))
        {
            capture.DataAvailable += (s, e) =>
            {
                writer.Write(e.Buffer, 0, e.BytesRecorded);
            };
            capture.RecordingStopped += (s, e) =>
            {
                writer.Dispose();
                capture.Dispose();
                Console.WriteLine($"Recording stopped. Audio saved to {outputFilePath}");
            };

            Console.WriteLine("Press Enter to start recording...");
            Console.ReadLine();
            capture.StartRecording();
            Console.WriteLine("Recording... Press Enter to stop.");
            Console.ReadLine();
            capture.StopRecording();
        }

        // Convert WAV to low-bitrate MP3 for speech-to-text using MediaFoundationEncoder
        string mp3FilePath = "loopback_capture.mp3";
        try
        {
            using (var reader = new AudioFileReader(outputFilePath))
            {
                var mediaType = MediaFoundationEncoder.SelectMediaType(
                    AudioSubtypes.MFAudioFormat_MP3,
                    reader.WaveFormat,
                    16000 // 16 kbps for speech
                );
                MediaFoundationEncoder.EncodeToMp3(reader, mp3FilePath, 16000); // 16 kbps
            }
            Console.WriteLine($"WAV converted to low-bitrate MP3: {mp3FilePath}");
            // Clean up the original WAV file if needed
            File.Delete(outputFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error converting to MP3: {ex.Message}");
        }

        // Send MP3 to GROQ Whisper API for transcription        
        if (string.IsNullOrEmpty(groqApiKey))
        {
            Console.WriteLine("GROQ_API_KEY environment variable not set.");
            return;
        }
        try
        {
            using (var httpClient = new System.Net.Http.HttpClient())
            using (var form = new System.Net.Http.MultipartFormDataContent())
            using (var fileStream = System.IO.File.OpenRead(mp3FilePath))
            using (var fileContent = new System.Net.Http.StreamContent(fileStream))
            {
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
                form.Add(new System.Net.Http.StringContent("whisper-large-v3"), "model");
                form.Add(fileContent, "file", "loopback_capture.mp3");
                form.Add(new System.Net.Http.StringContent("en"), "language");
                form.Add(new System.Net.Http.StringContent("verbose_json"), "response_format");

                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", groqApiKey);
                var response = httpClient.PostAsync("https://api.groq.com/openai/v1/audio/transcriptions", form).Result;
                var responseString = response.Content.ReadAsStringAsync().Result;
                if (response.IsSuccessStatusCode)
                {
                    // Print the full JSON response
                    Console.WriteLine("GROQ transcription response:");
                    Console.WriteLine(responseString);

                    // Show the transcribed text per segment (start time and text)
                    try
                    {
                        var json = System.Text.Json.JsonDocument.Parse(responseString);
                        if (json.RootElement.TryGetProperty("segments", out var segments))
                        {
                            Console.WriteLine("\nSegments:");
                            foreach (var segment in segments.EnumerateArray())
                            {
                                var start = segment.GetProperty("start").GetDouble();
                                var text = segment.GetProperty("text").GetString();
                                Console.WriteLine($"[{start,5:0.00}s] {text}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing segments: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine($"GROQ API error: {response.StatusCode}");
                    Console.WriteLine(responseString);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending MP3 to GROQ: {ex.Message}");
        }
    }
}