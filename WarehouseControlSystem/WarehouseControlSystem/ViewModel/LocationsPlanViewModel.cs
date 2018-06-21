﻿// ----------------------------------------------------------------------------------
// Copyright © 2018, Oleg Lobakov, Contacts: <oleg.lobakov@gmail.com>
// Licensed under the GNU GENERAL PUBLIC LICENSE, Version 3.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// https://github.com/OlegLobakov/WarehouseControlSystem/blob/master/LICENSE
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WarehouseControlSystem.ViewModel.Base;
using WarehouseControlSystem.Helpers.NAV;
using WarehouseControlSystem.Model;
using WarehouseControlSystem.Model.NAV;
using WarehouseControlSystem.View.Pages.LocationsScheme;
using WarehouseControlSystem.View.Pages.ZonesScheme;
using System.Collections.ObjectModel;
using Xamarin.Forms;
using WarehouseControlSystem.Resx;
using System.Windows.Input;
using System.Threading;

namespace WarehouseControlSystem.ViewModel
{
    public class LocationsPlanViewModel : PlanBaseViewModel
    {
        public LocationViewModel SelectedLocationViewModel { get; set; }
        public ObservableCollection<LocationViewModel> LocationViewModels { get; set; } = new ObservableCollection<LocationViewModel>();
        public ObservableCollection<LocationViewModel> SelectedViewModels { get; set; } = new ObservableCollection<LocationViewModel>();

        public ICommand ListLocationsCommand { protected set; get; }
        public ICommand NewLocationCommand { protected set; get; }
        public ICommand EditLocationCommand { protected set; get; }
        public ICommand DeleteLocationCommand { protected set; get; }

        public bool IsSelectedList { get { return SelectedViewModels.Count > 0; } }


        public LocationsPlanViewModel(INavigation navigation) : base(navigation)
        {
            ListLocationsCommand = new Command(ListLocations);
            NewLocationCommand = new Command(NewLocation);
            EditLocationCommand = new Command(EditLocation);
            DeleteLocationCommand = new Command(DeleteLocation);

            IsEditMode = true;
            Title = AppResources.LocationsSchemePage_Title;
            if (Global.CurrentConnection != null)
            {
                Title = Global.CurrentConnection.Name + " | " + AppResources.LocationsSchemePage_Title;
            }
            State = ModelState.Undefined;
            IsEditMode = false;
        }

        public void ClearAll()
        {
            SelectedViewModels.Clear();
            foreach (LocationViewModel lvm in LocationViewModels)
            {
                lvm.DisposeModel();
            }
            LocationViewModels.Clear();
        }

        public async void Load()
        {
            if (NotNetOrConnection)
            {
                return;
            }

            try
            {
                State = ModelState.Loading;
                PlanWidth = await NAV.GetPlanWidth(ACD.Default).ConfigureAwait(true);
                PlanHeight = await NAV.GetPlanHeight(ACD.Default).ConfigureAwait(true);
                CheckPlanSizes();
                List<Location> list = await NAV.GetLocationList("", true, 1, int.MaxValue, ACD.Default).ConfigureAwait(true); 
                if ((list is List<Location>) && (!IsDisposed))
                {
                    if (list.Count > 0)
                    {
                        ClearAll();
                        foreach (Location location in list)
                        {
                            SetDefaultSizes(location);
                            LocationViewModel lvm = new LocationViewModel(Navigation, location);
                            lvm.OnTap += Lvm_OnTap;
                            LocationViewModels.Add(lvm);
                        }
                        State = ModelState.Normal;
                        UpdateMinSizes();
                        Rebuild(true);
                    }
                    else
                    {
                        State = ModelState.Error;
                        ErrorText = "No Data";
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                ErrorText = e.Message;
            }
            catch (NAVErrorException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                State = ModelState.Error;
                ErrorText = AppResources.Error_LoadLocation + Environment.NewLine + e.Message;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                State = ModelState.Error;
                ErrorText = AppResources.Error_LoadLocation + Environment.NewLine + e.Message;
            }
        }

        int defaulttop;
        int defaultleft;
        int defaultwidth;
        int defaultheight; 

        private void CheckPlanSizes()
        {
            if (PlanWidth == 0)
            {
                PlanWidth = 20;
            }
            if (PlanHeight == 0)
            {
                PlanHeight = 10;
            }

            defaulttop = 1;
            defaultleft = 1;
            defaultwidth = Math.Max(1, (PlanWidth - 6) / 5);
            defaultheight = Math.Max(1, (PlanHeight - 5) / 4);
        }

        private void SetDefaultSizes(Location location)
        {
            if (location.Width == 0)
            {
                location.Left = defaulttop;
                location.Width = defaultwidth;
                location.Height = defaultheight;
                location.Top = defaulttop;

                defaultleft = defaultleft + defaultwidth + 1;
                if (defaultleft > (PlanWidth - defaultwidth))
                {
                    defaultleft = 1;
                    defaulttop = defaulttop + defaultheight + 1;
                }

                if (defaulttop > (PlanHeight - defaultheight))
                {
                    defaulttop = 1;
                }

                if (location.Left + location.Width > PlanWidth)
                {
                    PlanWidth += location.Left + location.Width - PlanWidth;
                }
                if (location.Top + location.Height > PlanHeight)
                {
                    PlanHeight += location.Top + location.Height - PlanHeight;
                }
            }
        }

        public async void LoadAll()
        {
            if (NotNetOrConnection)
            {
                return;
            }

            try
            {
                State = ModelState.Loading;
                List<Location> list = await NAV.GetLocationList("", false, 1, int.MaxValue, ACD.Default).ConfigureAwait(true);
                if ((!IsDisposed) && (list is List<Location>))
                {
                    if (list.Count > 0)
                    {
                        ClearAll();
                        foreach (Location location in list)
                        {
                            LocationViewModel lvm = new LocationViewModel(Navigation, location);
                            LocationViewModels.Add(lvm);
                        }
                        State = ModelState.Normal;
                    }
                    else
                    {
                        State = ModelState.Error;
                        ErrorText = "No Data";
                    }
                }
            }
            catch (OperationCanceledException e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                ErrorText = e.Message;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                ErrorText = e.Message;
                State = ModelState.Error;
                ErrorText = AppResources.Error_LoadLocationList;
            }
        }
        

        public override void Rebuild(bool recreate)
        {
            if ((!CanRebuildInterface) || (LocationViewModels.Count == 0))
            {
                return;
            }

            foreach (LocationViewModel lvm in LocationViewModels)
            {
                lvm.Left = lvm.Location.Left * WidthStep;
                lvm.Top = lvm.Location.Top * HeightStep;
                lvm.Width = lvm.Location.Width * WidthStep;
                lvm.Height = lvm.Location.Height * HeightStep;
            }

            if (recreate)
            {
                MessagingCenter.Send(this, "Rebuild");
            }
            else
            {
                MessagingCenter.Send(this, "Reshape");
            }
        }

        private async void Lvm_OnTap(LocationViewModel tappedlvm)
        {
            if (!IsEditMode)
            {
                Global.SearchLocationCode = tappedlvm.Code;
                try
                {
                    ZonesSchemePage zsp = new ZonesSchemePage(tappedlvm.Location);
                    await Navigation.PushAsync(zsp);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }
            else
            {
                foreach (LocationViewModel lvm in LocationViewModels)
                {
                    if (lvm != tappedlvm)
                    {
                        lvm.Selected = false;
                        lvm.EditMode = SchemeElementEditMode.None;
                    }
                }

                if (tappedlvm.Selected)
                {
                    switch (tappedlvm.EditMode)
                    {
                        case SchemeElementEditMode.None:
                            break;

                        case SchemeElementEditMode.Move:
                            tappedlvm.EditMode = SchemeElementEditMode.Resize;
                            break;

                        case SchemeElementEditMode.Resize:
                            tappedlvm.Selected = false;
                            tappedlvm.EditMode = SchemeElementEditMode.None;
                            break;
                        default:
                            throw new InvalidOperationException("LocationsViewModel Lvm_OnTap Impossible Value ");
                    }
                }
                else
                {
                    tappedlvm.Selected = true;
                    tappedlvm.EditMode = SchemeElementEditMode.Move;
                }

                SelectedViewModels = new ObservableCollection<LocationViewModel>(LocationViewModels.ToList().FindAll(x => x.Selected == true));
            }
        }

        public void UnSelectAll()
        {
            foreach (LocationViewModel lvm in LocationViewModels)
            {
                lvm.Selected = false;
            }
        }

        public async void ListLocations()
        {
            LocationListPage llp = new LocationListPage();
            await Navigation.PushAsync(llp);
        }

        public async void NewLocation()
        {
            Location newLocation = new Location();
            SelectedLocationViewModel = new LocationViewModel(Navigation, newLocation)
            {
                CreateMode = true
            };
            LocationViewModels.Add(SelectedLocationViewModel);
            LocationCardPage lnp = new LocationCardPage(SelectedLocationViewModel);
            await Navigation.PushAsync(lnp);
        }

        public async void EditLocation(object obj)
        {
            LocationViewModel lvm = (LocationViewModel)obj;
            if (lvm is LocationViewModel)
            {
                lvm.CreateMode = false;
                LocationCardPage lnp = new LocationCardPage(lvm);
                await Navigation.PushAsync(lnp);
            }
        }

        public async void DeleteLocation(object obj)
        {
            if (NotNetOrConnection)
            {
                return;
            }

            try
            {
                LocationViewModel lvm = (LocationViewModel)obj;
                State = ModelState.Loading;
                await NAV.DeleteLocation(lvm.Location.Code, ACD.Default);
                LocationViewModels.Remove(lvm);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                ErrorText = e.Message;
                State = ModelState.Error;
            }
            finally
            {
                State = ModelState.Normal;
                LoadAnimation = false;
            }

        }


        public async void SaveLocationChangesAsync()
        {
            if (NotNetOrConnection)
            {
                return;
            }

            List<LocationViewModel> list = LocationViewModels.ToList().FindAll(x => x.Selected == true);
            foreach (LocationViewModel lvm in list)
            {
                try
                {
                    await NAV.ModifyLocation(lvm.Location, ACD.Default);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    ErrorText = e.Message;
                }
            }
            UpdateMinSizes();
        }

        public async void SaveSchemeParams()
        {
            if (NotNetOrConnection)
            {
                return;
            }

            try
            {
                await NAV.SetPlanWidth(PlanWidth, ACD.Default);
                await NAV.SetPlanHeight(PlanHeight, ACD.Default);
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                ErrorText = e.Message;
            }
            UpdateMinSizes();
        }

        public void UpdateMinSizes()
        {
            int newminplanwidth = 0;
            int newminplanheight = 0;

            foreach (LocationViewModel lvm in LocationViewModels)
            {
                if (lvm.Location.Left + lvm.Location.Width > newminplanwidth)
                {
                    newminplanwidth = lvm.Location.Left + lvm.Location.Width;
                }
                if (lvm.Location.Top + lvm.Location.Height > newminplanheight)
                {
                    newminplanheight = lvm.Location.Top + lvm.Location.Height;
                }
            }
            MinPlanWidth = newminplanwidth;
            MinPlanHeight = newminplanheight;
        }

        public override void DisposeModel()
        {
            base.DisposeModel();
        }
    }
}