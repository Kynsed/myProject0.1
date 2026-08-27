using System;
using Microsoft.Xna.Framework.Input;

namespace Monocle
{
    public class VirtualButton : VirtualInput
    {
        public Binding Binding;
        public float Threshold;
        public float BufferTime;
        public int GamepadIndex;
        public Keys? DebugOverridePressed;

        private float firstRepeatTime;
        private float multiRepeatTime;
        private float bufferCounter;
        private float repeatCounter;
        private bool canRepeat;
        private bool consumed;

        public bool Repeating { get; private set; }

        public VirtualButton(Binding binding, int gamepadIndex, float bufferTime, float triggerThreshold)
        {
            Binding = binding;
            GamepadIndex = gamepadIndex;
            BufferTime = bufferTime;
            Threshold = triggerThreshold;
        }

        public VirtualButton()
        {
        }

        public void SetRepeat(float repeatTime)
        {
            SetRepeat(repeatTime, repeatTime);
        }

        public void SetRepeat(float firstRepeatTime, float multiRepeatTime)
        {
            this.firstRepeatTime = firstRepeatTime;
            this.multiRepeatTime = multiRepeatTime;
            canRepeat = this.firstRepeatTime > 0;
            if (!canRepeat)
                Repeating = false;
        }

        public override void Update()
        {
            consumed = false;
            bufferCounter -= Engine.DeltaTime;

            bool check = false;
            if (Binding.Pressed(GamepadIndex, Threshold))
            {
                bufferCounter = BufferTime;
                check = true;
            }
            else if (Binding.Check(GamepadIndex, Threshold))
                check = true;

            if (!check)
            {
                Repeating = false;
                repeatCounter = 0;
                bufferCounter = 0;
            }
            else if (canRepeat)
            {
                Repeating = false;
                if (repeatCounter == 0)
                    repeatCounter = firstRepeatTime;
                else
                {
                    repeatCounter -= Engine.DeltaTime;
                    if (repeatCounter <= 0)
                    {
                        Repeating = true;
                        repeatCounter = multiRepeatTime;
                    }
                }
            }
        }

        public bool Check
        {
            get
            {
                return !MInput.Disabled && Binding.Check(GamepadIndex, Threshold);
            }
        }

        public bool Pressed
        {
            get
            {
                if (DebugOverridePressed != null && MInput.Keyboard.Check(DebugOverridePressed.Value))
                    return true;

                return !MInput.Disabled && !consumed && (bufferCounter > 0 || Repeating || Binding.Pressed(GamepadIndex, Threshold));
            }
        }

        public bool Released
        {
            get
            {
                return !MInput.Disabled && Binding.Released(GamepadIndex, Threshold);
            }
        }

        public void ConsumeBuffer()
        {
            bufferCounter = 0;
        }

        public void ConsumePress()
        {
            bufferCounter = 0;
            consumed = true;
        }

        public static implicit operator bool(VirtualButton button)
        {
            return button.Check;
        }
    }
}
