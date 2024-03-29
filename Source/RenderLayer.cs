﻿using System;
using System.Collections.Generic;
using NbCore;
using NbCore.Common;
using NbCore.Platform.Windowing;
using NbCore.Systems;
using NbCore.UI.ImGui;

namespace NibbleEditor
{
    public class RenderLayer :ApplicationLayer
    {
        public RenderLayer(NbWindow win, Engine e) : base(win, e)
        {

        }

        public void OnResize(NbResizeArgs e)
        {
            //Update renderbuffer size
            EngineRef.GetSystem<RenderingSystem>().Resize(e.Width, e.Height);
        }

        public override void OnFrameUpdate(double dt)
        {
            //Enable Animation System
            //if (RenderState.settings.renderSettings.ToggleAnimations)
            EngineRef.GetSystem<AnimationSystem>().OnFrameUpdate(dt);

            //Update systems
            EngineRef.GetSystem<SceneManagementSystem>().OnFrameUpdate(dt);
            EngineRef.GetSystem<TransformationSystem>().OnFrameUpdate(dt);
            EngineRef.GetSystem<ScriptingSystem>().OnFrameUpdate(dt);
            
            //Enable Action System
            if (NbRenderState.settings.ViewSettings.EmulateActions)
                EngineRef.GetSystem<ActionSystem>().OnFrameUpdate(dt);

            //Post FrameUpdate Actions
            EngineRef.GetSystem<AnimationSystem>().OnPostFrameUpdate();
            
            //Camera & Light Positions
            //Update common transforms

        }

        public override void OnRenderFrameUpdate(double dt)
        {
            //Per Frame System Updates
            EngineRef.GetSystem<TransformationSystem>().OnRenderUpdate(dt);
            EngineRef.GetSystem<AnimationSystem>().OnRenderUpdate(dt);
            EngineRef.GetSystem<SceneManagementSystem>().OnRenderUpdate(dt);

            //Rendering
            EngineRef.GetSystem<RenderingSystem>().OnRenderUpdate(dt);
        }

    }
}
