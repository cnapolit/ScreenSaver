using SDL2;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Linq;
using static SDL2.SDL;

namespace ScreenSaver.Models
{
    public enum ControllerInputState
    {
        Pressed,
        Released
    }

    public enum ControllerInput
    {
        None,
        Start,
        Back,
        LeftStick,
        RightStick,
        LeftShoulder,
        RightShoulder,
        Guide,
        A,
        B,
        X,
        Y,
        DPadLeft,
        DPadRight,
        DPadUp,
        DPadDown,
        TriggerLeft,
        TriggerRight,
        LeftStickLeft,
        LeftStickRight,
        LeftStickUp,
        LeftStickDown,
        RightStickLeft,
        RightStickRight,
        RightStickUp,
        RightStickDown
    }

    public class GameController
    {
        public IntPtr Controller { get; }
        public int InstanceId { get; }

        public readonly Dictionary<ControllerInput, ControllerInputState> LastInputState = new Dictionary<ControllerInput, ControllerInputState>()
        {
            {  ControllerInput.A, ControllerInputState.Released },
            {  ControllerInput.B, ControllerInputState.Released },
            {  ControllerInput.Back, ControllerInputState.Released },
            {  ControllerInput.DPadDown, ControllerInputState.Released },
            {  ControllerInput.DPadLeft, ControllerInputState.Released },
            {  ControllerInput.DPadRight, ControllerInputState.Released },
            {  ControllerInput.DPadUp, ControllerInputState.Released },
            {  ControllerInput.Guide, ControllerInputState.Released },
            {  ControllerInput.LeftShoulder, ControllerInputState.Released },
            {  ControllerInput.LeftStick, ControllerInputState.Released },
            {  ControllerInput.LeftStickDown, ControllerInputState.Released },
            {  ControllerInput.LeftStickLeft, ControllerInputState.Released },
            {  ControllerInput.LeftStickRight, ControllerInputState.Released },
            {  ControllerInput.LeftStickUp, ControllerInputState.Released },
            {  ControllerInput.RightShoulder, ControllerInputState.Released },
            {  ControllerInput.RightStick, ControllerInputState.Released },
            {  ControllerInput.RightStickDown, ControllerInputState.Released },
            {  ControllerInput.RightStickLeft, ControllerInputState.Released },
            {  ControllerInput.RightStickRight, ControllerInputState.Released },
            {  ControllerInput.RightStickUp, ControllerInputState.Released },
            {  ControllerInput.Start, ControllerInputState.Released },
            {  ControllerInput.TriggerLeft, ControllerInputState.Released },
            {  ControllerInput.TriggerRight, ControllerInputState.Released },
            {  ControllerInput.X, ControllerInputState.Released },
            {  ControllerInput.Y, ControllerInputState.Released }
        };

        public GameController(int index)
        {
            Controller = SDL_GameControllerOpen(index);
            InstanceId = SDL_JoystickInstanceID(SDL_GameControllerGetJoystick(Controller));
        }


        private static readonly (SDL_GameControllerButton, ControllerInput)[] SDLToInputs = new[]
        {
            (SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A, ControllerInput.A),
            (SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B, ControllerInput.B),
            (SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK, ControllerInput.Back),
            (SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE, ControllerInput.Guide),
            (SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER, ControllerInput.LeftShoulder),
            (SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK, ControllerInput.LeftStick),
            (SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER, ControllerInput.RightShoulder),
            (SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK, ControllerInput.RightStick),
            (SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START, ControllerInput.Start),
            (SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X, ControllerInput.X),
            (SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y, ControllerInput.Y),
            (SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN, ControllerInput.DPadDown),
            (SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT, ControllerInput.DPadLeft),
            (SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT, ControllerInput.DPadRight),
            (SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP, ControllerInput.DPadUp)
        };

        private static readonly (SDL_GameControllerAxis, ControllerInput, ControllerInput)[] SDLToAxis = new[]
        {
            (SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX, ControllerInput.LeftStickLeft, ControllerInput.LeftStickRight),
            (SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY, ControllerInput.LeftStickUp, ControllerInput.LeftStickDown),
            (SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX, ControllerInput.RightStickLeft, ControllerInput.RightStickRight),
            (SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY, ControllerInput.RightStickUp, ControllerInput.RightStickDown),
            (SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT, ControllerInput.TriggerLeft, ControllerInput.None),
            (SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT, ControllerInput.TriggerRight, ControllerInput.None)
        };

        public bool ProcessState()
        {
            var buttonsChanged = SDLToInputs.Select(x => ProcessButtonState(SDL_GameControllerGetButton(Controller, x.Item1), x.Item2)).ToList();

            foreach (var (sdlAxis, input, negativeInput) in SDLToAxis)
            {
                var state = SDL_GameControllerGetAxis(Controller, sdlAxis);
                buttonsChanged.Add(ProcessAxisState(state, input, true));
                if (negativeInput != ControllerInput.None)
                {
                    buttonsChanged.Add(ProcessAxisState(state, negativeInput, false));
                }
            }

            return buttonsChanged.Any(b => b);
        }

        private bool ProcessButtonState(byte currentState, ControllerInput button)
            => ProcessState(currentState is 1, button);

        private bool ProcessAxisState(short currentState, ControllerInput button, bool positive)
            => ProcessState(positive ? currentState > 16383 : currentState < -16383, button);

        private bool ProcessState(bool pressed, ControllerInput button)
        {
            var buttonChanged = pressed != LastInputState[button] is ControllerInputState.Pressed;
            if (buttonChanged)
            {
                LastInputState[button] = pressed
                    ? ControllerInputState.Released
                    : ControllerInputState.Pressed;
            }
            return buttonChanged;
        }
    }
}
