namespace CC26
{
    // On hazard objects. The hazard decides what happens to the robot, so immunity checks live here too.
    public interface IHazard
    {
        void Apply(Robot robot);
    }
}
