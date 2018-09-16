// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using OpenTK.Graphics;
using osu.Framework.Allocation;
using osu.Game.Skinning;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Rulesets.Osu.Objects.Drawables.Pieces;
using System;

namespace osu.Game.Rulesets.Osu.Objects.Drawables
{
    /// <summary>
    /// Element for the Blinds mod drawing 2 black boxes covering the whole screen which resize inside a restricted area with some leniency.
    /// </summary>
    public class DrawableOsuBlinds : Container
    {
        /// <summary>
        /// Black background boxes behind blind panel textures.
        /// </summary>
        private Box box1, box2;
        private Sprite panelLeft, panelRight;
        private Sprite bgPanelLeft, bgPanelRight;

        private Drawable bgRandomNpc;
        private Drawable randomNpc;
        private const float npc_movement_start = 1.5f;
        private float npcPosition = npc_movement_start;
        private bool animatingNpc;
        private Random random;

        private ISkinSource skin;

        private float targetClamp = 1;
        private float target = 1;
        private readonly float easing = 1;

        private const float black_depth = 10;
        private const float bg_panel_depth = 8;
        private const float fg_panel_depth = 4;
        private const float npc_depth = 6;

        private readonly Container restrictTo;
        private readonly bool modEasy, modHardrock;

        /// <summary>
        /// <para>
        /// Percentage of playfield to extend blinds over. Basically moves the origin points where the blinds start.
        /// </para>
        /// <para>
        /// -1 would mean the blinds always cover the whole screen no matter health.
        /// 0 would mean the blinds will only ever be on the edge of the playfield on 0% health.
        /// 1 would mean the blinds are fully outside the playfield on 50% health.
        /// Infinity would mean the blinds are always outside the playfield except on 100% health.
        /// </para>
        /// </summary>
        private const float leniency = 0.1f;

        /// <summary>
        /// Multiplier for adding a gap when the Easy mod is also currently applied.
        /// </summary>
        private const float easy_position_multiplier = 0.95f;

        public DrawableOsuBlinds(Container restrictTo, bool hasEasy, bool hasHardrock)
        {
            this.restrictTo = restrictTo;

            modEasy = hasEasy;
            modHardrock = hasHardrock;
        }

        [BackgroundDependencyLoader]
        private void load(ISkinSource skin, TextureStore textures)
        {
            RelativeSizeAxes = Axes.Both;
            Width = 1;
            Height = 1;

            Add(box1 = new Box
            {
                Anchor = Anchor.TopLeft,
                Origin = Anchor.TopLeft,
                Colour = Color4.Black,
                RelativeSizeAxes = Axes.Y,
                Width = 0,
                Height = 1,
                Depth = black_depth
            });
            Add(box2 = new Box
            {
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
                Colour = Color4.Black,
                RelativeSizeAxes = Axes.Y,
                Width = 0,
                Height = 1,
                Depth = black_depth
            });

            Add(bgPanelLeft = new ModBlindsPanelSprite {
                Origin = Anchor.TopRight,
                Colour = Color4.Gray,
                Depth = bg_panel_depth + 1
            });
            Add(panelLeft = new ModBlindsPanelSprite {
                Origin = Anchor.TopRight,
                Depth = bg_panel_depth
            });

            Add(bgPanelRight = new ModBlindsPanelSprite {
                Origin = Anchor.TopLeft,
                Colour = Color4.Gray,
                Depth = fg_panel_depth + 1
            });
            Add(panelRight = new ModBlindsPanelSprite {
                Origin = Anchor.TopLeft,
                Depth = fg_panel_depth
            });


            random = new Random();
            Add(bgRandomNpc = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Colour = Color4.Black,
                Width = 512 * 0.4f,
                Height = 512 * 0.95f,
                RelativePositionAxes = Axes.Y,
                X = -512,
                Y = 0,
                Depth = black_depth
            });
            Add(new SkinnableDrawable("Play/Catch/fruit-catcher-idle", name => randomNpc = new Sprite
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Texture = textures.Get(name),
                Width = 512,
                Height = 512,
                RelativePositionAxes = Axes.Y,
                X = -512,
                Y = 0
            }) {
                Depth = npc_depth
            });

            this.skin = skin;
            skin.SourceChanged += skinChanged;
            PanelTexture = textures.Get("Play/osu/blinds-panel");
        }

        private void skinChanged()
        {
            PanelTexture = skin.GetTexture("Play/osu/blinds-panel");
        }

        private float applyGap(float value)
        {
            float ret;
            if (modEasy)
            {
                const float multiplier = 0.95f;
                ret = value * multiplier;
            }
            else if (modHardrock)
            {
                const float multiplier = 1.1f;
                ret = value * multiplier;
            }
            else
            {
                ret = value;
            }

            if (ret > targetClamp)
                return targetClamp;
            else if (ret < 0)
                return 0;
            else
                return ret;
        }

        private static float applyAdjustmentCurve(float value)
        {
            // lagrange polinominal for (0,0) (0.5,0.35) (1,1) should make a good curve
            return 0.6f * value * value + 0.4f * value;
        }

        protected override void Update()
        {
            float start = Parent.ToLocalSpace(restrictTo.ScreenSpaceDrawQuad.TopLeft).X;
            float end = Parent.ToLocalSpace(restrictTo.ScreenSpaceDrawQuad.TopRight).X;
            float rawWidth = end - start;
            start -= rawWidth * leniency * 0.5f;
            end += rawWidth * leniency * 0.5f;

            float width = (end - start) * 0.5f * applyAdjustmentCurve(applyGap(easing));
            // different values in case the playfield ever moves from center to somewhere else.
            box1.Width = start + width;
            box2.Width = DrawWidth - end + width;

            panelLeft.X = start + width;
            panelRight.X = end - width;
            bgPanelLeft.X = start;
            bgPanelRight.X = end;

            float adjustedNpcPosition = npcPosition * rawWidth;
            if (randomNpc != null)
                randomNpc.X = adjustedNpcPosition;
            bgRandomNpc.X = adjustedNpcPosition;
        }

        public void TriggerNpc()
        {
            if (animatingNpc)
                return;

            bool left = (random.Next() & 1) != 0;
            bool exit = (random.Next() & 1) != 0;
            float start, end;

            if (left)
            {
                start = -npc_movement_start;
                end = npc_movement_start;

                randomNpc.Scale = new OpenTK.Vector2(1, 1);
            }
            else
            {
                start = npc_movement_start;
                end = -npc_movement_start;

                randomNpc.Scale = new OpenTK.Vector2(-1, 1);
            }

            // depths for exit from the left and entry from the right
            if (left == exit)
            {
                ChangeChildDepth(bgPanelLeft, fg_panel_depth + 1);
                ChangeChildDepth(panelLeft, fg_panel_depth);

                ChangeChildDepth(bgPanelRight, bg_panel_depth + 1);
                ChangeChildDepth(panelRight, bg_panel_depth);
            }
            else // depths for entry from the left or exit from the right
            {
                ChangeChildDepth(bgPanelLeft, bg_panel_depth + 1);
                ChangeChildDepth(panelLeft, bg_panel_depth);

                ChangeChildDepth(bgPanelRight, fg_panel_depth + 1);
                ChangeChildDepth(panelRight, fg_panel_depth);
            }

            animatingNpc = true;
            npcPosition = start;
            this.TransformTo(nameof(npcPosition), end, 3000, Easing.OutSine).Finally(_ => animatingNpc = false);

            targetClamp = 1;
            this.Delay(600).TransformTo(nameof(targetClamp), 0.6f, 300).Delay(500).TransformTo(nameof(targetClamp), 1f, 300);

            randomNpc?.FadeIn(250).Delay(2000).FadeOut(500);
            bgRandomNpc.FadeIn(250).Delay(2000).FadeOut(500);
        }

        /// <summary>
        /// Health between 0 and 1 for the blinds to base the width on. Will get animated for 200ms using out-quintic easing.
        /// </summary>
        public float Value
        {
            set
            {
                target = value;
                this.TransformTo(nameof(easing), target, 200, Easing.OutQuint);
            }
            get
            {
                return target;
            }
        }

        public Texture PanelTexture
        {
            set
            {
                panelLeft.Texture = value;
                panelRight.Texture = value;
                bgPanelLeft.Texture = value;
                bgPanelRight.Texture = value;
            }
        }
    }
}
