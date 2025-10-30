// Services/IAnimalService.cs
using MVVM_Base.Model;
using System.Collections.Generic;

namespace MVVM_Base.Services
{
    public interface IAnimalService
    {
        IEnumerable<Animal> GetAllAnimals();
        void AddAnimal(Animal animal);
    }
}