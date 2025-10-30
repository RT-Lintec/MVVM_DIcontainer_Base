// Models/Dog.cs
namespace MVVM_Base.Model
{
    public class Dog : Animal
    {
        public string Breed { get; set; }

        public void Bark()
        {
            // 犬が吠える動作を模倣
        }
    }
}