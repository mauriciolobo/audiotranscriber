# audiocsharp

A simple C# console application to capture system audio ("what you hear"), save it as a WAV file, convert it to a low-bitrate MP3, and transcribe the audio using the GROQ Whisper API.

## Features
- **Loopback Audio Capture**: Records all audio currently playing on your Windows system using NAudio's `WasapiLoopbackCapture`.
- **WAV to MP3 Conversion**: Converts the captured WAV file to a 16kbps MP3 using NAudio's `MediaFoundationEncoder`.
- **Speech-to-Text Transcription**: Sends the MP3 file to the GROQ Whisper API and prints the transcription, including per-segment timestamps.

## Requirements
- .NET 9.0 SDK or later
- Windows OS (required for WASAPI loopback capture)
- [NAudio](https://github.com/naudio/NAudio) NuGet package (already referenced in the project)
- A valid GROQ API key for Whisper transcription

## Usage
1. **Build and Run**
   ```sh
   dotnet build
   dotnet run
   ```
2. **Recording**
   - Press Enter to start capturing system audio.
   - Press Enter again to stop recording.
   - The audio is saved as `loopback_capture.wav`.
3. **Conversion and Transcription**
   - The app converts the WAV to a low-bitrate MP3 (`loopback_capture.mp3`).
   - The MP3 is sent to the GROQ Whisper API for transcription.
   - The full JSON response and per-segment transcriptions are printed to the console.

## Configuration
- Set your GROQ API key in the code (see `groqApiKey` in `Program.cs`).

## Main Code Flow (`Program.cs`)
- Uses `WasapiLoopbackCapture` to record system audio.
- Writes audio data to a WAV file with `WaveFileWriter`.
- Converts WAV to MP3 using `MediaFoundationEncoder.EncodeToMp3`.
- Sends the MP3 to the GROQ Whisper API using `HttpClient` and prints the transcription.

## References
- [NAudio Documentation](https://github.com/naudio/NAudio/tree/master/Docs)
- [GROQ Whisper API](https://console.groq.com/docs/)

## License
This project is for demonstration purposes. See individual library licenses for details.
