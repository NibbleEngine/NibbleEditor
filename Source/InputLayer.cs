using System;
using System.Collections.Generic;
using NbCore;
using NbCore.Common;
using NbCore.Input;
using NbCore.Platform.Windowing;

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
            KeyboardState.SetKeyDownStatus(e.Key, false);
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

        
        
        public override void OnFrameUpdate(NbWindow win, double dt)
        {
            

        }

        public override void OnRenderFrameUpdate(NbWindow win, double dt)
        {
            
        }


    }
}
