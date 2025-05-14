using System.Numerics;
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
            AddChild(_bulletRender = new BulletRender
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
	// WWDP - UNDER CONSTRUCTION - TO BE REFACTORED
    public sealed class EnergyGunBatteryStatusControl : Control
    {
        private readonly EntityUid _gun;
        private readonly BaseBulletRenderer _bullets2;
        private readonly Label _ammoLabel;
        private readonly Label _heatLabel;
        private readonly Label _lampLabel;
        private readonly BatteryAmmoProviderComponent _component;
        private readonly GunTemperatureRegulatorComponent? _regcomp;
        private readonly GunTemperatureRegulatorSystem _regSys;
        private readonly IEntityManager _entMan;

        private int _ammoCount = 0;
        private float _heat = 0;


        public EnergyGunBatteryStatusControl(BatteryAmmoProviderComponent comp)
        {
            _entMan = IoCManager.Resolve<IEntityManager>();
            _regSys = _entMan.System<GunTemperatureRegulatorSystem>();
            _gun = comp.Owner;
            _component = comp;
            _ammoCount = comp.Shots;
            MinHeight = 15;
            HorizontalExpand = true;
            VerticalAlignment = Control.VAlignment.Center;
            AddChild(new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Children =
                {
                    (new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        Children =
                        {
                            (_lampLabel = new Label
                            {
                                StyleClasses = { StyleNano.StyleClassItemStatus },
                                HorizontalAlignment = HAlignment.Left,
                                VerticalAlignment = VAlignment.Bottom,
                                Text = $" ●"
                            }),
                            (_heatLabel = new Label
                            {
                                StyleClasses = { StyleNano.StyleClassItemStatus },
                                HorizontalAlignment = HAlignment.Right,
                                HorizontalExpand = true,
                                VerticalAlignment = VAlignment.Bottom,
                                Text = $"{_heat-273.15:0.00} °C"
                            }),
                        }
                    }),
                    (new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Horizontal,
                        Children =
                        {
                            (_bullets2 = new BarBulletRenderer
                            {
                                Rows = 4,
                                MaxWidth = 75
                            }),
                            (_ammoLabel = new Label
                            {
                                StyleClasses = { StyleNano.StyleClassItemStatus },
                                HorizontalExpand = true,
                                HorizontalAlignment = HAlignment.Right,
                                VerticalAlignment = VAlignment.Top,
                                Text = $"x{_ammoCount:00}"
                            }),
                        }
                    })
                }
            });

            // if temp regulator component is missing on the gun, hide the temperature gauge and lamp display
            // since they won't matter anyways
            if(!_entMan.TryGetComponent(comp.Owner, out _regcomp))
            {
                _heatLabel.Visible = false;
                _lampLabel.Visible = false;
                return;
            }
            _lampLabel.Visible = _regcomp.RequiresLamp;
        }

        private void UpdateTemp(float T)
        {
            _heatLabel.Text = $"{T - 273.15:0.00} °C";
            float hue = 0; // full red
            if (T < _regcomp!.TemperatureLimit) // assume _regcomp is not null since we'll check for it before calling this method
                hue = 0.66f - (T) / (_regcomp.TemperatureLimit) * 0.55f;
            // cyan at near 0K, orange at just below the safe temperature threshold, full red above threshold
            var tempColor = Color.FromHsv(new Robust.Shared.Maths.Vector4(hue, 1, 1, 1));
            _heatLabel.FontColorOverride = tempColor;
            _lampLabel.FontColorOverride = tempColor;
        }

        protected override void PreRenderChildren(ref ControlRenderArguments args)
        {
            if (_bullets2.Capacity != _component.Capacity)
                _bullets2.Capacity = _component.Capacity;

            if (_bullets2.Count != _component.Shots)
                _bullets2.Count = _component.Shots;

            if(_ammoCount != _component.Shots)
            {
                _ammoCount = _component.Shots;
                _ammoLabel.Text = $"x{_ammoCount:00}";
            }

            // skip all the temperature stuff if the related component is not present;
            if(_regcomp is null)
            {
                return;
            }

            if (_regSys.GetLamp(_gun, out var lampComp, _regcomp))
            {
                if (lampComp is null || !lampComp.Intact)
                {
                    _lampLabel.Text = " ◌";
                }
                else
                {
                    const int divisions = 7;
                    const float divisions_reverse = 1f / divisions;
                    float howFucked = MathHelper.Clamp01((_regcomp.CurrentTemperature - lampComp.SafeTemperature) / (lampComp.UnsafeTemperature - lampComp.SafeTemperature));
                    int howFuckedInt = Math.Min((int) (howFucked / divisions_reverse + 0.999), divisions); // instead of futher complicating the line above i'll just plop a min function here - i just need to offset the graph to the left
                    _lampLabel.Text = $"{(lampComp.Intact ? " ●" : " ○")} {new string('!', howFuckedInt)}{new string('.', divisions - howFuckedInt)}";
                }
            }

            if(_heat != _regcomp.CurrentTemperature)
            {
                _heat = _regcomp.CurrentTemperature;
                UpdateTemp(_heat);
            }
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
                        Margin = new Thickness(0, 0, 5, 0),
                        Children =
                        {
                            (_bulletRender = new BulletRender
                            {
                                HorizontalAlignment = HAlignment.Right,
                                VerticalAlignment = VAlignment.Bottom
                            }),
                            (_noMagazineLabel = new Label
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
                        Margin = new Thickness(0, 0, 0, 2),
                        Children =
                        {
                            (_ammoCount = new Label
                            {
                                StyleClasses = {StyleNano.StyleClassItemStatus},
                                HorizontalAlignment = HAlignment.Right,
                            }),
                            (_chamberedBullet = new TextureRect
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
            AddChild((_bulletsList = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Center,
                SeparationOverride = 0
            }));
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
                        TextureScale = new Vector2(scale, scale),
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

    #endregion
}
