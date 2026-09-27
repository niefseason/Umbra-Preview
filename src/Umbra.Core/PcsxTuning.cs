namespace Umbra.Core;

public sealed class PcsxTuning
{
    public int AudioBufferMs { get; set; } = 50;
    public bool ShowPerformance { get; set; }
    public void Validate()
    {
        if(AudioBufferMs is not (50 or 75 or 100 or 150))
            throw new UserError("pcsx-audio-buffer","Choose a supported PS2 audio buffer: 50, 75, 100 or 150 ms.");
    }
}
