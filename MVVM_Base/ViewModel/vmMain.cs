using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MVVM_Base.Model;
using MVVM_Base.Services;

using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace MVVM_Base.ViewModel
{
    public partial class vmMain : ObservableObject
    {
        private readonly IAnimalService _animalService;

        public vmMain(IAnimalService animalService)
        {
            _animalService = animalService;
            Animals = new ObservableCollection<Animal>(_animalService.GetAllAnimals());
        }

        [ObservableProperty]
        private UserControl _currentView;

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private int _age;

        [ObservableProperty]
        private string _breed;

        public ObservableCollection<Animal> Animals { get; }

        [RelayCommand]
        private void AddDog()
        {
            var dog = new Dog
            {
                Name = this.Name,
                Age = this.Age,
                Breed = this.Breed
            };

            _animalService.AddAnimal(dog);
            Animals.Add(dog);

            // 入力フィールドをクリア
            Name = string.Empty;
            Age = 0;
            Breed = string.Empty;
        }
    }
}