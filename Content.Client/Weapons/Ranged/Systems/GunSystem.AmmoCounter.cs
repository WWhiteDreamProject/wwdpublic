using Content.Client._White.Guns;
using Content.Client._White.UI;
using Content.Client.IoC;
using Content.Client.Items;
using Content.Client.Resources;
using Content.Client.Stylesheets;
using Content.Client.Weapons.Ranged.Components;
using Content.Client.Weapons.Ranged.ItemStatus;
using Content.Shared._White.Guns;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.Animations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using System.Numerics;

namespace Content.Client.Weapons.Ranged.Systems;

public sealed partial class GunSystem
{
    private void OnAmmoCounterCollect(EntityUid uid, AmmoCounterComponent component, ItemStatusCollectMessage args)
    {
        RefreshControl(uid, component);

        if (component.Control != null)
            args.Controls.Add(component.Control);
    }

    /// <summary>
    /// Refreshes the control being used to show ammo. Useful if you change the AmmoProvider.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    private void RefreshControl(EntityUid uid, AmmoCounterComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        component.Control?.Dispose();
        component.Control = null;

        var ev = new AmmoCounterControlEvent();
        RaiseLocalEvent(uid, ev, false);

        // Fallback to default if none specified
        ev.Control ??= new DefaultStatusControl();

        component.Control = ev.Control;
        UpdateAmmoCount(uid, component);
    }

    private void UpdateAmmoCount(EntityUid uid, AmmoCounterComponent component)
    {
        if (component.Control == null)
            return;

        var ev = new UpdateAmmoCounterEvent()
        {
            Control = component.Control
        };

        RaiseLocalEvent(uid, ev, false);
    }

    protected override void UpdateAmmoCount(EntityUid uid, bool prediction = true)
    {
        // Don't use resolves because the method is shared and there's no compref and I'm trying to
        // share as much code as possible
        if (prediction && !Timing.IsFirstTimePredicted ||
            !TryComp<AmmoCounterComponent>(uid, out var clientComp))
        {
            return;
        }

        UpdateAmmoCount(uid, clientComp);
    }

    /// <summary>
    /// Raised when an ammocounter is requesting a control.
    /// </summary>
    public sealed class AmmoCounterControlEvent : EntityEventArgs
    {
        public Control? Control;
    }

    /// <summary>
    /// Raised whenever the ammo count / magazine for a control needs updating.
    /// </summary>
    public sealed class UpdateAmmoCounterEvent : HandledEntityEventArgs
    {
        public Control Control = default!;
    }

    #region Controls

    private sealed class DefaultStatusControl : Control
    {
        private readonly BulletRender _bulletRender;

        public DefaultStatusControl()
        {
            MinHeight = 15;
            HorizontalExpand = true;
            VerticalAlignment = VAlignment.Center;
            AddChild(_bulletRender = new()
            {
                HorizontalAlignment = HAlignment.Right,
                VerticalAlignment = VAlignment.Bottom
            });
        }

        public void Update(int count, int capacity)
        {
            _bulletRender.Count = count;
            _bulletRender.Capacity = capacity;

            _bulletRender.Type = capacity > 50 ? BulletRender.BulletType.Tiny : BulletRender.BulletType.Normal;
        }
    }

    // WWDP - DEFUNCT - Left just in case for upstream compatibility
    public sealed class BoxesStatusControl : Control
    {
        private readonly BatteryBulletRenderer _bullets;
        private readonly Label _ammoCount;

        public BoxesStatusControl()
        {
            MinHeight = 15;
            HorizontalExpand = true;
            VerticalAlignment = Control.VAlignment.Center;

            AddChild(new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                Children =
                {
                    (_bullets = new()
                    {
                        Margin = new(0, 0, 5, 0),
                        HorizontalExpand = true
                    }),
                    (_ammoCount = new()
                    {
                        StyleClasses = { StyleNano.StyleClassItemStatus },
                        HorizontalAlignment = HAlignment.Right,
                        VerticalAlignment = VAlignment.Bottom
                    }),
                }
            });
        }

        public void Update(int count, int max)
        {
            _ammoCount.Visible = true;

            _ammoCount.Text = $"x{count:00}";

            _bullets.Capacity = max;
            _bullets.Count = count;
        }
    }

    private sealed class ChamberMagazineStatusControl : Control
    {
        private readonly BulletRender _bulletRender;
        private readonly TextureRect _chamberedBullet;
        private readonly Label _noMagazineLabel;
        private readonly Label _ammoCount;

        public ChamberMagazineStatusControl()
        {
            MinHeight = 15;
            HorizontalExpand = true;
            VerticalAlignment = Control.VAlignment.Center;

            AddChild(new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                Children =
                {
                    new Control
                    {
                        HorizontalExpand = true,
                        Margin = new(0, 0, 5, 0),
                        Children =
                        {
                            (_bulletRender = new()
                            {
                                HorizontalAlignment = HAlignment.Right,
                                VerticalAlignment = VAlignment.Bottom
                            }),
                            (_noMagazineLabel = new()
                            {
                                Text = "No Magazine!",
                                StyleClasses = {StyleNano.StyleClassItemStatus}
                            })
                        }
                    },
                    new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Vertical,
                        VerticalAlignment = VAlignment.Bottom,
                        Margin = new(0, 0, 0, 2),
                        Children =
                        {
                            (_ammoCount = new()
                            {
                                StyleClasses = {StyleNano.StyleClassItemStatus},
                                HorizontalAlignment = HAlignment.Right,
                            }),
                            (_chamberedBullet = new()
                            {
                                Texture = StaticIoC.ResC.GetTexture("/Textures/Interface/ItemStatus/Bullets/chambered.png"),
                                HorizontalAlignment = HAlignment.Left,
                            }),
                        }
                    }
                }
            });
        }

        public void Update(bool chambered, bool magazine, int count, int capacity)
        {
            _chamberedBullet.ModulateSelfOverride =
                chambered ? Color.FromHex("#d7df60") : Color.Black;

            if (!magazine)
            {
                _bulletRender.Visible = false;
                _noMagazineLabel.Visible = true;
                _ammoCount.Visible = false;
                return;
            }

            _bulletRender.Visible = true;
            _noMagazineLabel.Visible = false;
            _ammoCount.Visible = true;

            _bulletRender.Count = count;
            _bulletRender.Capacity = capacity;

            _bulletRender.Type = capacity > 50 ? BulletRender.BulletType.Tiny : BulletRender.BulletType.Normal;

            _ammoCount.Text = $"x{count:00}";
        }

        public void PlayAlarmAnimation(Animation animation)
        {
            _noMagazineLabel.PlayAnimation(animation, "alarm");
        }
    }

    private sealed class RevolverStatusControl : Control
    {
        private readonly BoxContainer _bulletsList;

        public RevolverStatusControl()
        {
            MinHeight = 15;
            HorizontalExpand = true;
            VerticalAlignment = Control.VAlignment.Center;
            AddChild(_bulletsList = new()
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Center,
                SeparationOverride = 0
            });
        }

        public void Update(int currentIndex, bool?[] bullets)
        {
            _bulletsList.RemoveAllChildren();
            var capacity = bullets.Length;

            string texturePath;
            if (capacity <= 20)
            {
                texturePath = "/Textures/Interface/ItemStatus/Bullets/normal.png";
            }
            else if (capacity <= 30)
            {
                texturePath = "/Textures/Interface/ItemStatus/Bullets/small.png";
            }
            else
            {
                texturePath = "/Textures/Interface/ItemStatus/Bullets/tiny.png";
            }

            var texture = StaticIoC.ResC.GetTexture(texturePath);
            var spentTexture = StaticIoC.ResC.GetTexture("/Textures/Interface/ItemStatus/Bullets/empty.png");

            FillBulletRow(currentIndex, bullets, _bulletsList, texture, spentTexture);
        }

        private void FillBulletRow(int currentIndex, bool?[] bullets, Control container, Texture texture, Texture emptyTexture)
        {
            var capacity = bullets.Length;
            var colorA = Color.FromHex("#b68f0e");
            var colorB = Color.FromHex("#d7df60");
            var colorSpentA = Color.FromHex("#b50e25");
            var colorSpentB = Color.FromHex("#d3745f");
            var colorGoneA = Color.FromHex("#000000");
            var colorGoneB = Color.FromHex("#222222");

            var altColor = false;
            var scale = 1.3f;

            for (var i = 0; i < capacity; i++)
            {
                var bulletFree = bullets[i];
                // Add a outline
                var box = new Control()
                {
                    MinSize = texture.Size * scale,
                };
                if (i == currentIndex)
                {
                    box.AddChild(new TextureRect
                    {
                        Texture = texture,
                        TextureScale = new(scale, scale),
                        ModulateSelfOverride = Color.LimeGreen,
                    });
                }
                Color color;
                Texture bulletTexture = texture;

                if (bulletFree.HasValue)
                {
                    if (bulletFree.Value)
                    {
                        color = altColor ? colorA : colorB;
                    }
                    else
                    {
                        color = altColor ? colorSpentA : colorSpentB;
                        bulletTexture = emptyTexture;
                    }
                }
                else
                {
                    color = altColor ? colorGoneA : colorGoneB;
                }

                box.AddChild(new TextureRect
                {
                    Stretch = TextureRect.StretchMode.KeepCentered,
                    Texture = bulletTexture,
                    ModulateSelfOverride = color,
                });
                altColor ^= true;
                container.AddChild(box);
            }
        }
    }

    // WWDP EDIT START
    // here be shitcode
    public sealed class EnergyGunBatteryStatusControl : Control
    {
        private readonly EntityUid _gun;
        private readonly BarControl _ammoBar;
        private readonly Label _ammoLabel;
        private readonly Label _heatLabel;
        private readonly Label _lampLabel;
        private readonly BatteryAmmoProviderComponent _ammoProvider;
        private readonly GunOverheatComponent? _regulator;
        private readonly GunOverheatSystem _regSys;

        private int _ammoCount;
        private bool _heatLimitEnabled = true;
        private float _heatLimit;
        private float _heat; // caching temperature and ammo counts so that the labels don't end up having their measures invalidated every frame
                                 // not sure if this makes any difference performance-wise, but it just seems like a good idea
        public EnergyGunBatteryStatusControl(EntityUid uid, BatteryAmmoProviderComponent comp)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            _regSys = entMan.System<GunOverheatSystem>();
            _gun = uid;
            _ammoProvider = comp;
            _ammoCount = comp.Shots;
            MinHeight = 15;
            HorizontalExpand = true;
            VerticalAlignment = VAlignment.Center;
            AddChild(new BoxContainer // outer box
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Children =
                {
                    new BoxContainer // inner upper box, lamp indicator and temp gauge
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        Children =
                        {
                            (_lampLabel = new()
                            {
                                StyleClasses = { StyleNano.StyleClassItemStatus },
                                HorizontalAlignment = HAlignment.Left,
                                VerticalAlignment = VAlignment.Bottom,
                                Text = " \u25cf"
                            }),
                            (_heatLabel = new()
                            {
                                StyleClasses = { StyleNano.StyleClassItemStatus },
                                HorizontalAlignment = HAlignment.Right,
                                HorizontalExpand = true,
                                VerticalAlignment = VAlignment.Bottom,
                                Text = $"{_heat-273.15:0.00} °C "
                            }),
                        }
                    },
                    new BoxContainer // inner lower box, the ammo display and counter
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        Children =
                        {
                            (_ammoBar = new()
                            {
                                Rows = 4,
                                MaxWidth = 75
                            }),
                            (_ammoLabel = new()
                            {
                                StyleClasses = { StyleNano.StyleClassItemStatus },
                                HorizontalExpand = true,
                                HorizontalAlignment = HAlignment.Right,
                                VerticalAlignment = VAlignment.Top,
                                Text = $"x{_ammoCount:00}"
                            }),
                        }
                    }
                }
            });

            // if temp regulator component is missing on the gun, hide the temperature gauge and lamp display
            // since they won't matter anyways
            if (!entMan.TryGetComponent(_gun, out _regulator))
            {
                _heatLabel.Visible = false;
                _lampLabel.Visible = false;
                return;
            }
            _lampLabel.Visible = _regulator.RequiresLamp;
        }

        // still using kelvin because having temperature go from 0 to +inf is much nicer than from -273.15 to +inf
        private void UpdateTemp(float K)
        {
            var celcius = K - 273.15f;
            // we assume _regulator is not null since we'll check for it before calling this method
            var maxTemp = _regulator!.MaxDisplayTemperatureCelcius;
            var currentTemp = celcius > maxTemp ? $"{maxTemp:0}+°C" : $"{celcius:0} °C";
            _heatLabel.Text = _regulator.SafetyEnabled 
                ? $"{currentTemp}/{_regulator.TemperatureLimit - 273.15f:0} °C " 
                : currentTemp;

            float hue = 0; // full red
            const float hueoffset = 0.07f; // raises the 0K color from dark blue to a brighter tone

            if (K < _regulator.TemperatureLimit)
                hue = 0.66f - (K / _regulator.TemperatureLimit * 0.55f * (1f - hueoffset) + hueoffset);

            var tempColor = Color.FromHsv(new(hue, 1, 1, 1));
            _heatLabel.FontColorOverride = tempColor;
            _lampLabel.FontColorOverride = tempColor;
        }

        protected override void PreRenderChildren(ref ControlRenderArguments args)
        {
            _ammoBar.Fill = 0;
            if (_ammoProvider.Capacity > 0)
                _ammoBar.Fill = (float) _ammoProvider.Shots / _ammoProvider.Capacity;

            if (_ammoCount != _ammoProvider.Shots)
            {
                _ammoCount = _ammoProvider.Shots;
                _ammoLabel.Text = $"x{_ammoCount:00}";
            }

            // skip all the temperature stuff if the related component is not present;
            if (_regulator is null)
                return;

            if (_regSys.GetLamp((_gun,_regulator), out var lamp))
                _lampLabel.Text = !lamp.Value.Comp.Intact ? " ◌" : " ●";

            if (_heat != _regulator.CurrentTemperature || _heatLimit != _regulator.TemperatureLimit || _heatLimitEnabled != _regulator.SafetyEnabled)
            {
                _heatLimit = _regulator.TemperatureLimit;
                _heat = _regulator.CurrentTemperature;
                _heatLimitEnabled = _regulator.SafetyEnabled;
                UpdateTemp(_heat);
            }
        }
    }
    // WWDP EDIT END
    #endregion
}
