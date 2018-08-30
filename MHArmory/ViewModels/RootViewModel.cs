﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using MHArmory.Configurations;
using MHArmory.Core;
using MHArmory.Core.DataStructures;
using MHArmory.Search;

namespace MHArmory.ViewModels
{
    public class RootViewModel : ViewModelBase, IDisposable
    {
        public ICommand OpenSkillSelectorCommand { get; }
        public ICommand SearchArmorSetsCommand { get; }
        public ICommand CancelArmorSetsSearchCommand { get; }
        public ICommand AdvancedSearchCommand { get; }
        public ICommand OpenDecorationsOverrideCommand { get; }
        public ICommand OpenSearchResultProcessingCommand { get; }

        public ICommand AboutCommand { get; }

        public event EventHandler AbilitiesChanged;

        public ISolverData SolverData { get; private set; }

        private Solver solver;

        private bool isDataLoading = true;
        public bool IsDataLoading
        {
            get { return isDataLoading; }
            set { SetValue(ref isDataLoading, value); }
        }

        private bool isDataLoaded;
        public bool IsDataLoaded
        {
            get { return isDataLoaded; }
            set { SetValue(ref isDataLoaded, value); }
        }

        public AdvancedSearchViewModel AdvancedSearchViewModel { get; } = new AdvancedSearchViewModel();

        private IEnumerable<AbilityViewModel> selectedAbilities;
        public IEnumerable<AbilityViewModel> SelectedAbilities
        {
            get { return selectedAbilities; }
            set { SetValue(ref selectedAbilities, value); }
        }

        public SearchResultProcessingViewModel SearchResultProcessing { get; }

        internal void NotifyConfigurationLoaded()
        {
            SearchResultProcessing.NotifyConfigurationLoaded();
            InParameters.NotifyConfigurationLoaded();
        }

        private IEnumerable<ArmorSetViewModel> rawFoundArmorSets;

        private IEnumerable<ArmorSetViewModel> foundArmorSets;
        public IEnumerable<ArmorSetViewModel> FoundArmorSets
        {
            get { return foundArmorSets; }
            private set { SetValue(ref foundArmorSets, value); }
        }

        public InParametersViewModel InParameters { get; }

        private bool isSearching;
        public bool IsSearching
        {
            get { return isSearching; }
            private set { SetValue(ref isSearching, value); }
        }

        private bool isAutoSearch;
        public bool IsAutoSearch
        {
            get { return isAutoSearch; }
            set { SetValue(ref isAutoSearch, value); }
        }

        public RootViewModel()
        {
            OpenSkillSelectorCommand = new AnonymousCommand(OpenSkillSelector);
            SearchArmorSetsCommand = new AnonymousCommand(SearchArmorSets);
            CancelArmorSetsSearchCommand = new AnonymousCommand(CancelArmorSetsSearchForCommand);
            AdvancedSearchCommand = new AnonymousCommand(AdvancedSearch);
            OpenDecorationsOverrideCommand = new AnonymousCommand(OpenDecorationsOverride);
            OpenSearchResultProcessingCommand = new AnonymousCommand(OpenSearchResultProcessing);

            AboutCommand = new AnonymousCommand(OnAbout);

            SearchResultProcessing = new SearchResultProcessingViewModel(this);

            InParameters = new InParametersViewModel(this);
        }

        public void Dispose()
        {
            if (loadoutManager != null)
            {
                loadoutManager.LoadoutChanged -= LoadoutManager_LoadoutChanged;
                loadoutManager.ModifiedChanged -= LoadoutManager_ModifiedChanged;
            }
        }

        private void OnAbout()
        {
            var sb = new StringBuilder();

            App.GetAssemblyInfo(sb);

            System.Windows.MessageBox.Show(sb.ToString(), "About MHArmory", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        public void ApplySorting(bool force, int limit = 200)
        {
            if (rawFoundArmorSets == null)
                return;

            IEnumerable<ArmorSetViewModel> result = rawFoundArmorSets;

            if (SearchResultProcessing.ApplySort(ref result, force, limit))
                FoundArmorSets = result;
        }

        private LoadoutManager loadoutManager;

        public void SetLoadoutManager(LoadoutManager loadoutManager)
        {
            if (this.loadoutManager != null)
            {
                this.loadoutManager.LoadoutChanged -= LoadoutManager_LoadoutChanged;
                this.loadoutManager.ModifiedChanged -= LoadoutManager_ModifiedChanged;
            }

            this.loadoutManager = loadoutManager;

            if (this.loadoutManager != null)
            {
                this.loadoutManager.LoadoutChanged += LoadoutManager_LoadoutChanged;
                this.loadoutManager.ModifiedChanged += LoadoutManager_ModifiedChanged;
            }
        }

        private string loadoutText;
        public string LoadoutText
        {
            get { return loadoutText; }
            private set { SetValue(ref loadoutText, value); }
        }

        private void UpdateLoadoutText()
        {
            LoadoutText = $"{loadoutManager.CurrentLoadoutName ?? "(no loadout)"}{(loadoutManager.IsModified ? " *" : string.Empty)}";
        }

        private void LoadoutManager_LoadoutChanged(object sender, LoadoutNameEventArgs e)
        {
            UpdateLoadoutText();
        }

        private void LoadoutManager_ModifiedChanged(object sender, EventArgs e)
        {
            UpdateLoadoutText();
        }

        private void OpenSkillSelector(object parameter)
        {
            RoutedCommands.OpenSkillsSelector.ExecuteIfPossible(null);
        }

        private void AdvancedSearch(object parameter)
        {
            RoutedCommands.OpenAdvancedSearch.ExecuteIfPossible(null);
        }

        private void OpenDecorationsOverride()
        {
            RoutedCommands.OpenDecorationsOverride.ExecuteIfPossible(null);
        }

        private void OpenSearchResultProcessing()
        {
            RoutedCommands.OpenSearchResultProcessing.ExecuteIfPossible(null);
        }

        public async void SearchArmorSets()
        {
            try
            {
                await SearchArmorSetsInternal();
            }
            catch (TaskCanceledException)
            {
            }
        }

        private async void CancelArmorSetsSearchForCommand()
        {
            await CancelArmorSetsSearch();
        }

        public async Task CancelArmorSetsSearch()
        {
            if (searchCancellationTokenSource != null)
            {
                if (searchCancellationTokenSource.IsCancellationRequested)
                    return;

                searchCancellationTokenSource.Cancel();

                if (previousSearchTask != null)
                {
                    try
                    {
                        await previousSearchTask;
                    }
                    catch
                    {
                    }
                }
            }
        }

        private CancellationTokenSource searchCancellationTokenSource;
        private Task previousSearchTask;

        private async Task SearchArmorSetsInternal()
        {
            await CancelArmorSetsSearch();

            searchCancellationTokenSource = new CancellationTokenSource();
            previousSearchTask = Task.Run(() => SearchArmorSetsInternal(searchCancellationTokenSource.Token));

            IsSearching = true;

            try
            {
                await previousSearchTask;
            }
            finally
            {
                IsSearching = false;

                previousSearchTask = null;
                searchCancellationTokenSource = null;
            }
        }

        public void CreateSolverData()
        {
            SolverData = null;

            if (IsDataLoaded == false || SelectedAbilities == null)
                return;

            var desiredAbilities = SelectedAbilities
                .Where(x => x.IsChecked)
                .Select(x => x.Ability)
                .ToList();

            SolverData = new SolverData2(
                InParameters.Slots.Select(x => x.Value).ToList(),
                GlobalData.Instance.Heads.Where(x => x.Rarity <= InParameters.Rarity),
                GlobalData.Instance.Chests.Where(x => x.Rarity <= InParameters.Rarity),
                GlobalData.Instance.Gloves.Where(x => x.Rarity <= InParameters.Rarity),
                GlobalData.Instance.Waists.Where(x => x.Rarity <= InParameters.Rarity),
                GlobalData.Instance.Legs.Where(x => x.Rarity <= InParameters.Rarity),
                GlobalData.Instance.Charms.Where(x => x.Rarity <= InParameters.Rarity),
                GlobalData.Instance.Jewels.Where(x => x.Rarity <= InParameters.Rarity).Select(CreateSolverDataJewelModel),
                desiredAbilities
            );

            SolverData.Done();

            /*************************************************************/
            var metrics = new SearchMetrics
            {
                Heads = SolverData.AllHeads.Count(x => x.IsSelected),
                Chests = SolverData.AllChests.Count(x => x.IsSelected),
                Gloves = SolverData.AllGloves.Count(x => x.IsSelected),
                Waists = SolverData.AllWaists.Count(x => x.IsSelected),
                Legs = SolverData.AllLegs.Count(x => x.IsSelected),
                Charms = SolverData.AllCharms.Count(x => x.IsSelected),
                MinSlotSize = SolverData.MinJewelSize,
                MaxSlotSize = SolverData.MaxJewelSize,
            };

            metrics.UpdateCombinationCount();

            SearchMetrics = metrics;
            /*************************************************************/

            UpdateAdvancedSearch();

            rawFoundArmorSets = null;
        }

        public void UpdateAdvancedSearch()
        {
            ISolverData solverData = SolverData;

            var armorPieceTypesViewModels = new ArmorPieceTypesViewModel[]
            {
                new ArmorPieceTypesViewModel(solverData.AllHeads),
                new ArmorPieceTypesViewModel(solverData.AllChests),
                new ArmorPieceTypesViewModel(solverData.AllGloves),
                new ArmorPieceTypesViewModel(solverData.AllWaists),
                new ArmorPieceTypesViewModel(solverData.AllLegs),
                new ArmorPieceTypesViewModel(solverData.AllCharms)
            };

            AdvancedSearchViewModel.Update(armorPieceTypesViewModels);
        }

        private async Task SearchArmorSetsInternal(CancellationToken cancellationToken)
        {
            solver = new Solver(SolverData);

            solver.SearchMetricsChanged += SolverSearchMetricsChanged;

            IList<ArmorSetSearchResult> result = await solver.SearchArmorSets(cancellationToken);

            if (result == null)
            {
                //rawFoundArmorSets = null;
                //FoundArmorSets = null;
            }
            else
            {
                rawFoundArmorSets = result.Where(x => x.IsMatch).Select(x => new ArmorSetViewModel(
                    SolverData,
                    x.ArmorPieces,
                    x.Charm,
                    x.Jewels.Select(j => new ArmorSetJewelViewModel(j.Jewel, j.Count)).ToList(),
                    x.SpareSlots
                ));

                ApplySorting(true);
            }

            solver.SearchMetricsChanged -= SolverSearchMetricsChanged;
        }

        private SolverDataJewelModel CreateSolverDataJewelModel(IJewel jewel)
        {
            DecorationOverrideConfiguration decorationOverrideConfig = GlobalData.Instance.Configuration.InParameters?.DecorationOverride;

            if (decorationOverrideConfig != null && decorationOverrideConfig.UseOverride)
            {
                Dictionary<int, DecorationOverrideConfigurationItem> decoOverrides = decorationOverrideConfig?.Items;

                if (decoOverrides != null)
                {
                    if (decoOverrides.TryGetValue(jewel.Id, out DecorationOverrideConfigurationItem found) && found.IsOverriding)
                        return new SolverDataJewelModel(jewel, found.Count);
                }
            }

            return new SolverDataJewelModel(jewel, int.MaxValue);
        }

        internal void SelectedAbilitiesChanged()
        {
            AbilitiesChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SolverSearchMetricsChanged(SearchMetrics metricsData)
        {
            Dispatcher.BeginInvoke((Action)delegate ()
            {
                SearchMetrics = null;
                SearchMetrics = metricsData;
            });
        }

        private SearchMetrics searchMetrics;
        public SearchMetrics SearchMetrics
        {
            get { return searchMetrics; }
            private set
            {
                searchMetrics = value;
                NotifyPropertyChanged();
            }
        }
    }
}
