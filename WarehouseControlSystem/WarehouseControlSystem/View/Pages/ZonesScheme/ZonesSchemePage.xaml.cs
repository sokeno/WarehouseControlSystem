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
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using WarehouseControlSystem.Model;
using WarehouseControlSystem.Model.NAV;
using WarehouseControlSystem.Resx;
using WarehouseControlSystem.ViewModel;
using WarehouseControlSystem.View.Pages.Find;

namespace WarehouseControlSystem.View.Pages.ZonesScheme
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ZonesSchemePage : ContentPage
    {
        List<ZoneView> Views { get; set; }
        List<ZoneView> SelectedViews { get; set; }

        MovingActionTypeEnum MovingAction = MovingActionTypeEnum.None;

        TapGestureRecognizer TapGesture;
        PanGestureRecognizer PanGesture;

        private readonly ZonesPlanViewModel model;

        public ZonesSchemePage(Location location)
        {
            model = new ZonesPlanViewModel(Navigation, location);
            BindingContext = model;
            InitializeComponent();

            Views = new List<ZoneView>();
            SelectedViews = new List<ZoneView>();

            TapGesture = new TapGestureRecognizer();
            abslayout.GestureRecognizers.Add(TapGesture);

            PanGesture = new PanGestureRecognizer();
            abslayout.GestureRecognizers.Add(PanGesture);

            Title = AppResources.ZoneSchemePage_Title + " - " + location.Name;
            Global.CurrentLocationName = location.Name;

            MessagingCenter.Subscribe<ZonesPlanViewModel>(this, "Rebuild", Rebuild);
            MessagingCenter.Subscribe<ZonesPlanViewModel>(this, "Reshape", Reshape);

            model.IsEditMode = false;
            model.SetEditModeForItems(model.IsEditMode);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            PanGesture.PanUpdated += OnPaned;
            TapGesture.Tapped += GridTapped;
            await model.Load();
        }

        protected override void OnDisappearing()
        {
            model.State = ViewModel.Base.ModelState.Undefined;
            PanGesture.PanUpdated -= OnPaned;
            TapGesture.Tapped -= GridTapped;
            SelectedViews.Clear();
            Views.Clear();
            abslayout.Children.Clear();
            base.OnDisappearing();
        }

        protected override bool OnBackButtonPressed()
        {
            model.DisposeModel();
            MessagingCenter.Unsubscribe<ZonesPlanViewModel>(this, "Rebuild");
            MessagingCenter.Unsubscribe<ZonesPlanViewModel>(this, "Reshape");
            base.OnBackButtonPressed();
            return false;
        }

        private void StackLayout_SizeChanged(object sender, EventArgs e)
        {
            StackLayout sl = (StackLayout)sender;
            model.SetScreenSizes(sl.Width, sl.Height, false);
        }

        private void Abslayout_SizeChanged(object sender, EventArgs e)
        {
            AbsoluteLayout al = (AbsoluteLayout)sender;
            model.SetScreenSizes(al.Width, al.Height, true);
        }

        private void Rebuild(ZonesPlanViewModel lmv)
        {
            SelectedViews.Clear();
            abslayout.Children.Clear();
            Views.Clear();
            foreach (ZoneViewModel zvm in model.ZoneViewModels)
            {
                ZoneView zv = new ZoneView(zvm);
                AbsoluteLayout.SetLayoutBounds(zv, new Rectangle(zvm.Left, zvm.Top, zvm.Width, zvm.Height));
                abslayout.Children.Add(zv);
                Views.Add(zv);
                zvm.LoadRacks();
            }
        }

        private void Reshape(ZonesPlanViewModel rsmv)
        {
            foreach (ZoneView lv in Views)
            {
                AbsoluteLayout.SetLayoutBounds(lv, new Rectangle(lv.Model.Left, lv.Model.Top, lv.Model.Width, lv.Model.Height));
            }
        }

        private void GridTapped(object sender, EventArgs e)
        {
            foreach (ZoneView zv in Views)
            {
                zv.Opacity = 1;
            }
            model.UnSelectAll();
        }

        readonly Easing easing1 = Easing.Linear;
        readonly Easing easingParcking = Easing.CubicInOut;

        double x = 0, y = 0, widthstep = 0, heightstep = 0;

        double leftborder = double.MaxValue;
        double topborder = double.MaxValue;
        double rightborder = double.MinValue;
        double bottomborder = double.MinValue;

        double oldeTotalX = 0, oldeTotalY = 0;

        private async void OnPaned(object sender, PanUpdatedEventArgs e)
        {
            if (!model.IsEditMode)
            {
                return;
            }

            if ((MovingAction != MovingActionTypeEnum.None) && (MovingAction != MovingActionTypeEnum.Pan))
            {
                return;
            }

            if (!model.IsSelectedList)
            {
                return;
            }

            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    {
                        SelectedViews = Views.FindAll(x => x.Model.Selected == true);
                        MovingAction = MovingActionTypeEnum.Pan;

                        widthstep = (model.ScreenWidth/ model.PlanWidth);
                        heightstep = (model.ScreenHeight / model.PlanHeight);

                        leftborder = double.MaxValue;
                        topborder = double.MaxValue;
                        rightborder = double.MinValue;
                        bottomborder = double.MinValue;

                        foreach (ZoneView zv in SelectedViews)
                        {
                            leftborder = Math.Min(zv.X, leftborder);
                            topborder = Math.Min(zv.Y, topborder);
                            rightborder = Math.Max(zv.X + zv.Width, rightborder);
                            bottomborder = Math.Max(zv.Y + zv.Height, bottomborder);
                            zv.Opacity = 0.5;
                            zv.Model.SavePrevSize(zv.Width, zv.Height);
                        }

                        x += oldeTotalX;
                        y += oldeTotalY;
                        break;
                    }
                case GestureStatus.Running:
                    {
                        double dx = x + e.TotalX;
                        double dy = y + e.TotalY;

                        oldeTotalX = e.TotalX;
                        oldeTotalY = e.TotalY;

                        if (dx + leftborder < 0)
                        {
                            dx = -leftborder;
                        }

                        if (dx + rightborder > model.ScreenWidth)
                        {
                            dx = model.ScreenWidth - rightborder;
                        }

                        if (dy + topborder < 0)
                        {
                            dy = -topborder;
                        }

                        if (dy + bottomborder > model.ScreenHeight)
                        {
                            dy = model.ScreenHeight - bottomborder;
                        }

                        foreach (ZoneView zv in SelectedViews)
                        {
                            if (zv.Model.EditMode == SchemeElementEditMode.Move)
                            {
                                await zv.TranslateTo(dx, dy, 250, easing1);
                            }
                            if (zv.Model.EditMode == SchemeElementEditMode.Resize)
                            {
                                AbsoluteLayout.SetLayoutBounds(zv, new Rectangle(zv.X, zv.Y, zv.Model.PrevWidth + dx, zv.Model.PrevHeight + dy));
                            }
                        }
                        break;
                    }
                case GestureStatus.Completed:
                    {

                        x = 0;
                        y = 0;
                        oldeTotalX = 0;
                        oldeTotalY = 0;
                        foreach (ZoneView zv in SelectedViews)
                        {
                            if (zv.Model.EditMode == SchemeElementEditMode.Move)
                            {
                                double newX = zv.X + zv.TranslationX;
                                double newY = zv.Y + zv.TranslationY;

                                zv.Model.Zone.Left = (int)Math.Round(newX / widthstep);
                                zv.Model.Zone.Top = (int)Math.Round(newY / heightstep);

                                //выравнивание по сетке
                                double dX = zv.Model.Zone.Left * widthstep - zv.X;
                                double dY = zv.Model.Zone.Top * heightstep - zv.Y;

                                await zv.TranslateTo(dX, dY, 500, easingParcking);
                                AbsoluteLayout.SetLayoutBounds(zv, new Rectangle(zv.X + dX, zv.Y + dY, zv.Width, zv.Height));
                                zv.TranslationX = 0;
                                zv.TranslationY = 0;
                            }
                            if (zv.Model.EditMode == SchemeElementEditMode.Resize)
                            {
                                zv.Model.Zone.Width = (int)Math.Round(zv.Width / widthstep);
                                zv.Model.Zone.Height = (int)Math.Round(zv.Height / heightstep);
                                double newWidth = zv.Model.Zone.Width * widthstep;
                                double newheight = zv.Model.Zone.Height * heightstep;
                                AbsoluteLayout.SetLayoutBounds(zv, new Rectangle(zv.X, zv.Y, newWidth, newheight));
                            }
                            zv.Opacity = 1;
                        }
                        await model.SaveZonesChangesAsync();
                        MovingAction = MovingActionTypeEnum.None;
                        break;
                    }
                case GestureStatus.Canceled:
                    {
                        MovingAction = MovingActionTypeEnum.None;
                        break;
                    }
                default:
                    throw new InvalidOperationException("ZonesSchemePage OnPaned Impossible Value ");
            }
        }

        private async void ToolbarItem_Search(object sender, EventArgs e)
        {
            SearchViewModel svm = new SearchViewModel(Navigation);
            FindPage fp = new FindPage(svm);
            await Navigation.PushAsync(fp);
        }

        private async void ToolbarItem_Clicked(object sender, EventArgs e)
        {
            GridTapped(null, new EventArgs());

            if (model.IsEditMode)
            {
                model.IsEditMode = false;
                model.SetEditModeForItems(model.IsEditMode);
                abslayout.BackgroundColor = Color.White;
                await model.SaveLocationParams();
            }
            else
            {
                abslayout.BackgroundColor = Color.LightGray;
                model.IsEditMode = true;
                model.SetEditModeForItems(model.IsEditMode);
            }
        }
    }
}