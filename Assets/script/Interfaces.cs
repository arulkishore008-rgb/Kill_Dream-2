using UnityEngine;

public class Interfaces 
{
   public interface Ishockable
    {
        void Recieveshock(float schockpower);
    }

    public interface Iburst
    {
        void Recieveburst(float burstpower);
    }

    public interface Iweapon
    {
        void useweapon();   
    }
}
