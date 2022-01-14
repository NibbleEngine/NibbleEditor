using System;
using System.Collections.Generic;
using OpenTK.Windowing.Common;
using NbCore;
using NbCore.Common;
using NbCore.Input;

namespace NibbleEditor
{

    public class InputLayer : ApplicationLayer
    {
        private static readonly Dictionary<OpenTK.Windowing.GraphicsLibraryFramework.Keys, NbKey> OpenTKKeyMap = new()
        {
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.A, NbKey.A },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.B, NbKey.B },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.C, NbKey.C },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.D, NbKey.D },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.E, NbKey.E },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.F, NbKey.F },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.G, NbKey.G },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.H, NbKey.H },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.I, NbKey.I },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.J, NbKey.J },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.K, NbKey.K },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.L, NbKey.L },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.M, NbKey.M },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.N, NbKey.N },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.O, NbKey.O },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.P, NbKey.P },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Q, NbKey.Q },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.R, NbKey.R },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.S, NbKey.S },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.T, NbKey.T },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.U, NbKey.U },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.V, NbKey.V },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.W, NbKey.W },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.X, NbKey.X },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Y, NbKey.Y },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Z, NbKey.Z },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Left, NbKey.LeftArrow },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Right, NbKey.RightArrow },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Up, NbKey.UpArrow },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Down, NbKey.DownArrow },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftAlt, NbKey.LeftAlt },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightAlt, NbKey.RightAlt },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftControl, NbKey.LeftCtrl },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightControl, NbKey.RightCtrl },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.LeftSuper, NbKey.LeftSuper },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.RightSuper, NbKey.RightSuper },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Backspace, NbKey.Backspace },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space, NbKey.Space },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Home, NbKey.Home },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.End, NbKey.End },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Insert, NbKey.Insert },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Delete, NbKey.Delete },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.PageUp, NbKey.PageUp },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.PageDown, NbKey.PageDown },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Enter, NbKey.Enter },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape, NbKey.Escape },
            { OpenTK.Windowing.GraphicsLibraryFramework.Keys.KeyPadEnter, NbKey.KeyPadEnter },
        };

        //Mouse States
        private NbMouseState currentMouseState = new();
        private NbMouseState prevMouseState = new();

        //Keyboard State
        private new NbKeyboardState KeyboardState;

        //Input
        public BaseGamepadHandler gpHandler;

        //Camera Stuff
        public CameraPos targetCameraPos;


        public InputLayer(Engine e) : base(e)
        {

        }

        #region EventHandlers

        public void OnKeyDown(KeyboardKeyEventArgs e)
        {
            //TODO: Make sure all keys are mapped so that we don't need to check everytime
            if (OpenTKKeyMap.ContainsKey(e.Key))
            {
                KeyboardState.SetKeyDownStatus(OpenTKKeyMap[e.Key], true);
            }
        }

        public void OnKeyUp(KeyboardKeyEventArgs e)
        {
            //TODO: Make sure all keys are mapped so that we don't need to check everytime
            if (OpenTKKeyMap.ContainsKey(e.Key))
            {
                KeyboardState.SetKeyDownStatus(OpenTKKeyMap[e.Key], false);
            }
        }

        public void OnMouseDown(MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left:
                    currentMouseState.SetButtonStatus(NbMouseButton.LEFT, true);
                    break;
                case OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right:
                    currentMouseState.SetButtonStatus(NbMouseButton.RIGHT, true);
                    break;
                case OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle:
                    currentMouseState.SetButtonStatus(NbMouseButton.MIDDLE, true);
                    break;
            }
        }

        public void OnMouseUp(MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left:
                    currentMouseState.SetButtonStatus(NbMouseButton.LEFT, false);
                    break;
                case OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Right:
                    currentMouseState.SetButtonStatus(NbMouseButton.RIGHT, false);
                    break;
                case OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Middle:
                    currentMouseState.SetButtonStatus(NbMouseButton.MIDDLE, false);
                    break;
            }
        }

        public void OnMouseMove(MouseMoveEventArgs e)
        {
            currentMouseState.Position.X = e.X;
            currentMouseState.Position.Y = e.Y;
            currentMouseState.PositionDelta.X = e.X - prevMouseState.Position.X;
            currentMouseState.PositionDelta.Y = e.Y - prevMouseState.Position.Y;
        }

        public void OnMouseWheel(MouseWheelEventArgs e)
        {
            currentMouseState.Scroll.X += e.OffsetX;
            currentMouseState.Scroll.Y += e.OffsetY;
        }

        public void OnCaptureInputChanged(bool state)
        {
            currentMouseState.UpdateScene = state;
            KeyboardState.UpdateScene = state;
        }

        #endregion

        
        #region INPUT_HANDLERS

        //Gamepad handler
        private void gamepadController()
        {
            if (gpHandler == null) return;
            if (!gpHandler.isConnected()) return;

            //Camera Movement
            float cameraSensitivity = 2.0f;
            float x, y, z, rotx, roty;

            x = gpHandler.getAction(ControllerActions.MOVE_X);
            y = gpHandler.getAction(ControllerActions.ACCELERATE) - gpHandler.getAction(ControllerActions.DECELERATE);
            z = gpHandler.getAction(ControllerActions.MOVE_Y_NEG) - gpHandler.getAction(ControllerActions.MOVE_Y_POS);
            rotx = -cameraSensitivity * gpHandler.getAction(ControllerActions.CAMERA_MOVE_H);
            roty = cameraSensitivity * gpHandler.getAction(ControllerActions.CAMERA_MOVE_V);

            targetCameraPos.PosImpulse.X = x;
            targetCameraPos.PosImpulse.Y = y;
            targetCameraPos.PosImpulse.Z = z;
            targetCameraPos.Rotation.X = rotx;
            targetCameraPos.Rotation.Y = roty;
        }

        //Keyboard handler
        private int keyDownStateToInt(NbKey k)
        {
            bool state = KeyboardState.IsKeyDown(k);
            return state ? 1 : 0;
        }

        public void UpdateInput()
        {
            bool kbStateUpdated = false;
            bool msStateUpdated = false;

            //Reset Mouse Inputs
            targetCameraPos.Reset();

            if (KeyboardState.UpdateScene)
            {
                kbStateUpdated = true;
                keyboardController();
            }

            if (currentMouseState.UpdateScene)
            {
                msStateUpdated = true;
                mouseController();
            }

            //TODO: Re-add controller support

            if (kbStateUpdated || msStateUpdated)
                Camera.CalculateNextCameraState(RenderState.activeCam, targetCameraPos);

            //gpController();

        }

        #endregion

        private void keyboardController()
        {
            //Camera Movement
            float step = 0.002f;
            float x, y, z;

            x = keyDownStateToInt(NbKey.D) - keyDownStateToInt(NbKey.A);
            y = keyDownStateToInt(NbKey.W) - keyDownStateToInt(NbKey.S);
            z = keyDownStateToInt(NbKey.R) - keyDownStateToInt(NbKey.F);

            //Camera rotation is done exclusively using the mouse

            //rotx = 50 * step * (kbHandler.getKeyStatus(OpenTK.Input.Key.E) - kbHandler.getKeyStatus(OpenTK.Input.Key.Q));
            //float roty = (kbHandler.getKeyStatus(Key.C) - kbHandler.getKeyStatus(Key.Z));

            RenderState.rotAngles.Y += 100 * step * (keyDownStateToInt(NbKey.E) - keyDownStateToInt(NbKey.Q));
            RenderState.rotAngles.Y %= 360;

            //Move Camera
            targetCameraPos.PosImpulse.X = x;
            targetCameraPos.PosImpulse.Y = y;
            targetCameraPos.PosImpulse.Z = z;
        }

        //Mouse Methods

        public void mouseController()
        {
            //targetCameraPos.Rotation.Xy += new Vector2(0.55f, 0);
            if (currentMouseState.IsButtonDown(NbMouseButton.LEFT))
            {
                OpenTK.Mathematics.Vector2 deltaVec = new(currentMouseState.PositionDelta.X,
                    currentMouseState.PositionDelta.Y);

                //Log("Mouse Delta {0} {1}", deltax, deltay);
                targetCameraPos.Rotation.X = deltaVec.X;
                targetCameraPos.Rotation.Y = deltaVec.Y;
            }
        }

        public override void OnFrameUpdate(ref Queue<object> data, double dt)
        {
            NbMouseState copymouse = currentMouseState;
            NbKeyboardState copykb = KeyboardState;
            data.Enqueue(copymouse);
            data.Enqueue(copykb);

            UpdateInput();

            //Reset Mouse State
            prevMouseState = currentMouseState;
            currentMouseState.PositionDelta.X = 0.0f;
            currentMouseState.PositionDelta.Y = 0.0f;
            currentMouseState.Scroll.X = 0.0f;
            currentMouseState.Scroll.Y = 0.0f;
        
        }

        public override void OnRenderFrameUpdate(ref Queue<object> data, double dt)
        {
            
        }


    }
}
