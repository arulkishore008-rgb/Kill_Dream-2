using UnityEngine;

public class Interfaces : MonoBehaviour
{
   public interface Ishockable
    {
        void Recieveshock(float schockpower);
    }

    public interface Iweapon
    {
        void useweapon();
    }
}
