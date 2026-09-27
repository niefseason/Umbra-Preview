namespace Umbra.Core;

public sealed class ModernStickTuning
{
    public int HorizontalPercent {get;set;}=100;
    public int VerticalPercent {get;set;}=100;
    public int MovementThreshold {get;set;}=8000;
    public int? CameraDeadzone {get;set;}
    public void Validate()
    {
        if(HorizontalPercent is <50 or >200 || VerticalPercent is <50 or >200 ||
            MovementThreshold is <2000 or >24000 || CameraDeadzone is <0 or >24000)
            throw new UserError("stick-tuning","Stick tuning is outside the supported range. Reset the game's stick tuning.");
    }
    public int Peak(int deadzone,bool vertical)
    {
        Validate();
        if(deadzone is <0 or >=32768)throw new UserError("stick-deadzone","Camera deadzone must be below 32,768.");
        return deadzone+(int)Math.Round((32768-deadzone)*100.0/(vertical?VerticalPercent:HorizontalPercent));
    }
}
