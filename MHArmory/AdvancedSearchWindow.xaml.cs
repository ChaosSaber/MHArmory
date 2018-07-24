﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MHArmory.Core.DataStructures;
using MHArmory.Search;
using MHArmory.ViewModels;

namespace MHArmory
{
    /// <summary>
    /// Interaction logic for AdvancedSearchWindow.xaml
    /// </summary>
    public partial class AdvancedSearchWindow : Window
    {
        private readonly RootViewModel root;
        private readonly SolverData solverData;
        private readonly AdvancedSearchViewModel advancedSearchViewModel;

        public AdvancedSearchWindow(RootViewModel root, SolverData solverData)
        {
            InitializeComponent();

            this.root = root;
            this.solverData = solverData;

            var armorPieceTypesViewModels = new ArmorPieceTypesViewModel[]
            {
                new ArmorPieceTypesViewModel(solverData.AllHeads),
                new ArmorPieceTypesViewModel(solverData.AllChests),
                new ArmorPieceTypesViewModel(solverData.AllGloves),
                new ArmorPieceTypesViewModel(solverData.AllWaists),
                new ArmorPieceTypesViewModel(solverData.AllLegs),
                new ArmorPieceTypesViewModel(solverData.AllCharms)
            };

            advancedSearchViewModel = new AdvancedSearchViewModel(armorPieceTypesViewModels);

            DataContext = advancedSearchViewModel;
        }
    }
}
