using System;
using System.Collections.Generic;
using NbCore;
using NbCore.Common;
using NbCore.Input;

namespace NibbleEditor
{

    public class InputLayer : ApplicationLayer
    {
        

        //Mouse States
        private NbMouseState currentMouseState = new();
        private NbMouseState prevMouseState = new();

        //Keyboard State
        private NbKeyboardState KeyboardState;

        //Input
        public BaseGamepadHandler gpHandler;

        //Camera Stuff
        public CameraPos targetCameraPos;


        public InputLayer(Engine e) : base(e)
        {

        }

        #region EventHandlers

        public void OnKeyDown(NbCore.Platform.Windowing.NbKeyArgs e)
        {
            //TODO: Make sure all keys are mapped so that we don't need to check everytime
            if (NbCore.Platform.Windowing.NbKeyArgs.SupportedKeys.Contains(e.Key))
            {
                KeyboardState.SetKeyDownStatus(e.Key, true);
            }
        }

        public void OnKeyUp(NbCore.Platform.Windowing.NbKeyArgs e)
        {
            //TODO: Make sure all keys are mapped so that we don't need to check everytime
            if (NbCore.Platform.Windowing.NbKeyArgs.SupportedKeys.Contains(e.Key))
            {
                KeyboardState.SetKeyDownStatus(e.Key, false);
            }
        }

        public void OnMouseDown(NbCore.Platform.Windowing.NbMouseButtonArgs e)
        {
            currentMouseState.SetButtonStatus(e.Button, true);
        }

        public void OnMouseUp(NbCore.Platform.Windowing.NbMouseButtonArgs e)
        {
            currentMouseState.SetButtonStatus(e.Button, false);
        }

        public void OnMouseMove(NbCore.Platform.Windowing.NbMouseMoveArgs e)
        {
            currentMouseState.Position.X = e.X;
            currentMouseState.Position.Y = e.Y;
            currentMouseState.PositionDelta.X = e.X - prevMouseState.Position.X;
            currentMouseState.PositionDelta.Y = e.Y - prevMouseState.Position.Y;
        }

        public void OnMouseWheel(NbCore.Platform.Windowing.NbMouseWheelArgs e)
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
                NbCore.Math.NbVector2 deltaVec = new(currentMouseState.PositionDelta.X,
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
