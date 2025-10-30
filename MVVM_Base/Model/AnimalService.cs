// Services/AnimalService.cs
using MVVM_Base.Model;
using System.Collections.Generic;
using System.Linq;

namespace MVVM_Base.Services
{
    public class AnimalService : IAnimalService
    {
        private readonly List<Animal> _animals = new List<Animal>();

        public IEnumerable<Animal> GetAllAnimals()
        {
            return _animals.ToList();
        }

        public void AddAnimal(Animal animal)
        {
            _animals.Add(animal);
        }
    }
}