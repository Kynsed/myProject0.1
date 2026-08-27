using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Input;

namespace Monocle
{
    [Serializable]
    public class Binding
    {
        public List<Keys> Keyboard = new List<Keys>();
        public List<Buttons> Controller = new List<Buttons>();

        [XmlIgnore]
        public List<Binding> ExclusiveFrom = new List<Binding>();

        public bool HasInput
        {
            get { return Keyboard.Count > 0 || Controller.Count > 0; }
        }

        public bool Add(params Keys[] keys)
        {
            bool added = false;
            foreach (Keys key in keys)
            {
                if (Keyboard.Contains(key))
                    continue;

                bool neededElsewhere = false;
                foreach (Binding other in ExclusiveFrom)
                    if (other.Needs(key))
                    {
                        neededElsewhere = true;
                        break;
                    }
                if (neededElsewhere)
                    continue;

                Keyboard.Add(key);
                added = true;
            }
            return added;
        }

        public bool Add(params Buttons[] buttons)
        {
            bool added = false;
            foreach (Buttons button in buttons)
            {
                if (Controller.Contains(button))
                    continue;

                bool neededElsewhere = false;
                foreach (Binding other in ExclusiveFrom)
                    if (other.Needs(button))
                    {
                        neededElsewhere = true;
                        break;
                    }
                if (neededElsewhere)
                    continue;

                Controller.Add(button);
                added = true;
            }
            return added;
        }

        public bool Needs(Buttons button)
        {
            if (!Controller.Contains(button))
                return false;
            if (Controller.Count <= 1)
                return true;
            if (!IsExclusive(button))
                return false;

            foreach (Buttons other in Controller)
                if (other != button && IsExclusive(other))
                    return false;
            return true;
        }

        public bool Needs(Keys key)
        {
            if (!Keyboard.Contains(key))
                return false;
            if (Keyboard.Count <= 1)
                return true;
            if (!IsExclusive(key))
                return false;

            foreach (Keys other in Keyboard)
                if (other != key && IsExclusive(other))
                    return false;
            return true;
        }

        public bool IsExclusive(Buttons button)
        {
            foreach (Binding other in ExclusiveFrom)
                if (other.Controller.Contains(button))
                    return false;
            return true;
        }

        public bool IsExclusive(Keys key)
        {
            foreach (Binding other in ExclusiveFrom)
                if (other.Keyboard.Contains(key))
                    return false;
            return true;
        }

        public bool ClearKeyboard()
        {
            if (ExclusiveFrom.Count > 0)
            {
                if (Keyboard.Count <= 1)
                    return false;

                int keep = 0;
                for (int i = 1; i < Keyboard.Count; i++)
                    if (IsExclusive(Keyboard[i]))
                        keep = i;

                Keys keepKey = Keyboard[keep];
                Keyboard.Clear();
                Keyboard.Add(keepKey);
            }
            else
                Keyboard.Clear();

            return true;
        }

        public bool ClearGamepad()
        {
            if (ExclusiveFrom.Count > 0)
            {
                if (Controller.Count <= 1)
                    return false;

                int keep = 0;
                for (int i = 1; i < Controller.Count; i++)
                    if (IsExclusive(Controller[i]))
                        keep = i;

                Buttons keepButton = Controller[keep];
                Controller.Clear();
                Controller.Add(keepButton);
            }
            else
                Controller.Clear();

            return true;
        }

        public float Axis(int gamepadIndex, float threshold)
        {
            foreach (Keys key in Keyboard)
                if (MInput.Keyboard.Check(key))
                    return 1f;

            foreach (Buttons button in Controller)
            {
                float axis = MInput.GamePads[gamepadIndex].Axis(button, threshold);
                if (axis != 0)
                    return axis;
            }

            return 0;
        }

        public bool Check(int gamepadIndex, float threshold)
        {
            for (int i = 0; i < Keyboard.Count; i++)
                if (MInput.Keyboard.Check(Keyboard[i]))
                    return true;

            for (int i = 0; i < Controller.Count; i++)
                if (MInput.GamePads[gamepadIndex].Check(Controller[i], threshold))
                    return true;

            return false;
        }

        public bool Pressed(int gamepadIndex, float threshold)
        {
            for (int i = 0; i < Keyboard.Count; i++)
                if (MInput.Keyboard.Pressed(Keyboard[i]))
                    return true;

            for (int i = 0; i < Controller.Count; i++)
                if (MInput.GamePads[gamepadIndex].Pressed(Controller[i], threshold))
                    return true;

            return false;
        }

        public bool Released(int gamepadIndex, float threshold)
        {
            for (int i = 0; i < Keyboard.Count; i++)
                if (MInput.Keyboard.Released(Keyboard[i]))
                    return true;

            for (int i = 0; i < Controller.Count; i++)
                if (MInput.GamePads[gamepadIndex].Released(Controller[i], threshold))
                    return true;

            return false;
        }

        public static void SetExclusive(params Binding[] list)
        {
            for (int i = 0; i < list.Length; i++)
                list[i].ExclusiveFrom.Clear();

            foreach (Binding a in list)
                foreach (Binding b in list)
                    if (a != b)
                    {
                        a.ExclusiveFrom.Add(b);
                        b.ExclusiveFrom.Add(a);
                    }
        }
    }
}
