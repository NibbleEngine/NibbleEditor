using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiCore = ImGuiNET.ImGui;
using NbCore;
using NbCore.Common;
using NibbleEditor;
using System.Runtime.InteropServices;
using ImGuiNET;
using System.Runtime.CompilerServices;
using NbCore.Platform.Windowing;

namespace NbCore.UI.ImGui
{
    
    public class ImGuiSceneGraphViewer
    {
        private SceneGraphNode _root = null;
        private SceneGraphNode _selected = null;
        private SceneGraphNode _clicked = null;
        private AppImGuiManager _manager = null;

        //Private imgui state
        private SceneGraphNode new_node = null;
        private bool open_add_sphere_popup = false;
        private bool entity_added = false;
        private ulong _dragged_node_id = 0;

        //primitive_add_props
        private int divs = 10;
        private float radius = 0.0f;
        private float height = 0.0f;
        private float width = 0.0f;

        
        //Inline AddChild Function
        private static void AddChild(SceneGraphNode m, SceneGraphNode n) => m.Children.Add(n);

        public ImGuiSceneGraphViewer(AppImGuiManager manager)
        {
            _manager = manager;
        }
        
        public void Traverse_Init(SceneGraphNode m)
        {
            foreach (SceneGraphNode child in m.Children)
                Traverse_Init(child);
        }
        
        public void Clear()
        {
            _root = null;
            _selected = null;
            _clicked = null;
        }

        public void Init(SceneGraphNode root)
        {
            Clear();

            //Setup root
            _root = root;
        }

        public void DrawModals()
        {
            if (open_add_sphere_popup)
            {
                ImGuiCore.OpenPopup("AddSpherePopup");
                open_add_sphere_popup = false;
            }

            //Process Modals
            //Modals
            bool isOpen = true;
            ImGuiCore.SetNextWindowSize(new Vector2(300, 60));
            if (ImGuiCore.BeginPopupModal("AddSpherePopup", ref isOpen, ImGuiNET.ImGuiWindowFlags.NoResize))
            {
                ImGuiCore.Columns(2);
                ImGuiCore.Text("Detail");
                ImGuiCore.NextColumn();
                ImGuiCore.DragInt("##sphereBands", ref divs, 1.0f, 10, 100);
                ImGuiCore.Columns(1);
                ImGuiCore.SameLine();
                if (ImGuiCore.Button("Add"))
                {
                    //Create Mesh
                    Primitives.Sphere sph = new(new(0.0f), 1.0f, divs);

                    NbMeshData md = sph.geom.GetMeshData();
                    NbMeshMetaData mmd = sph.geom.GetMetaData();
                    sph.Dispose();

                    NbMesh nm = new()
                    {
                        Hash = (ulong)mmd.GetHashCode(),
                        MetaData = mmd,
                        Data = md,
                        Material = _manager.EngineRef.GetMaterialByName("defaultMat")
                    };

                    //Create and register locator node
                    new_node = _manager.EngineRef.CreateMeshNode("Sphere#1", nm);
                    entity_added = true;
                    Callbacks.Log(this, "Creating Sphere Mesh Node", 
                        LogVerbosityLevel.INFO);
                    ImGuiCore.CloseCurrentPopup();
                }
                ImGuiCore.EndPopup();
            }

            if (entity_added)
            {
                //Register new node to engine
                _manager.EngineRef.AddSceneGraphNode(new_node, null, _clicked);
                _clicked.IsOpen = true; //Make sure to open the node so that the new node is visible

                //Set Reference to the new node
                _clicked = new_node;
                _manager.SetObjectReference(new_node);
                _manager.SetActiveMaterial(new_node);
            }
        }

        private int DrawChildren(SceneGraphNode node)
        {
            int index = 0;
            while (index < node.Children.Count)
            {
                DrawNode(node.Children[index]);
                index++;
            }
            return index;
        }
            
        public void Draw()
        {
            entity_added = false;
            DrawNode(_root);
            DrawModals();
        }

        private void DrawNode(SceneGraphNode n)
        {
            //Draw using ImGUI
            ImGuiNET.ImGuiTreeNodeFlags base_flags = ImGuiNET.ImGuiTreeNodeFlags.OpenOnArrow | 
                                                     ImGuiNET.ImGuiTreeNodeFlags.SpanAvailWidth | 
                                                     ImGuiNET.ImGuiTreeNodeFlags.AllowItemOverlap;
            
            if (n.Children.Count == 0)
                base_flags |= ImGuiNET.ImGuiTreeNodeFlags.NoTreePushOnOpen | ImGuiNET.ImGuiTreeNodeFlags.Leaf;

            if (_clicked != null && n == _clicked)
            {
                base_flags |= ImGuiNET.ImGuiTreeNodeFlags.Selected;
                _selected = n;
            }


            if (ImGuiCore.Checkbox("##Entity" + n.ID, ref n.IsRenderable))
            {
                n.SetRenderableStatusRec(n.IsRenderable);
            }
            ImGuiCore.SameLine();

            ImGuiCore.SetNextItemOpen(n.IsOpen);
            bool node_open = ImGuiCore.TreeNodeEx(n.ID.ToString(), base_flags, n.Name);

            n.IsOpen = node_open;
            if (ImGuiCore.IsItemClicked(ImGuiNET.ImGuiMouseButton.Left))
            {
                _clicked = n;
                _manager.SetObjectReference(n);
                _manager.SetActiveMaterial(n);
                ImGuiCore.CloseCurrentPopup();
            }

            if (ImGuiCore.BeginPopupContextItem()) // <-- use last item id as popup id
            {
                //Right click also counts as selection
                _clicked = n;
                _manager.SetObjectReference(n);
                _manager.SetActiveMaterial(n);
                
                if (ImGuiCore.BeginMenu("Add Child Node##child-ctx"))
                {
                    if (ImGuiCore.MenuItem("Add Locator"))
                    {
                        //Create and register locator node
                        new_node = _manager.EngineRef.CreateLocatorNode("Locator#1");
                        Callbacks.Log(this, "Creating Locator node", 
                            LogVerbosityLevel.INFO);
                        entity_added = true;
                    }
                    
                    if (ImGuiCore.MenuItem("Add Light"))
                    {
                        //Create and register locator node
                        new_node = _manager.EngineRef.CreateLightNode("Light#1", new NbVector3(1.0f), 100.0f, 1.0f);

                        Callbacks.Log(this, "Creating Light node", 
                            LogVerbosityLevel.INFO);
                        entity_added = true;
                    }

                    if (ImGuiCore.MenuItem("Add Sphere"))
                    {
                        open_add_sphere_popup = true;
                        radius = 10.0f; //Default value
                    }

                    if (ImGuiCore.MenuItem("Add Box"))
                    {
                        //Box Requires No parameters create it immediately

                        //Create Mesh
                        Primitives.Box bx = new(1.0f,1.0f,1.0f, new NbVector3(1.0f), true);

                        NbMeshData md = bx.geom.GetMeshData();
                        NbMeshMetaData mmd = bx.geom.GetMetaData();
                        bx.Dispose();

                        NbMesh nm = new()
                        {
                            Hash = (ulong)mmd.GetHashCode(),
                            MetaData = mmd,
                            Data = md,
                            Material = _manager.EngineRef.GetMaterialByName("defaultMat")
                        };

                        //Create and register locator node
                        new_node = _manager.EngineRef.CreateMeshNode("Box#1", nm);
                        entity_added = true;
                        Callbacks.Log(this, "Creating Box Mesh Node", LogVerbosityLevel.INFO);

                    }

                    if (ImGuiCore.MenuItem("Add Quad"))
                    {
                        //Box Requires No parameters create it immediately

                        //Create Mesh
                        Primitives.Quad q = new(1.0f, 1.0f);

                        NbMeshData md = q.geom.GetMeshData();
                        NbMeshMetaData mmd = q.geom.GetMetaData();
                        q.Dispose();

                        NbMesh nm = new()
                        {
                            Hash = (ulong)mmd.GetHashCode(),
                            MetaData = mmd,
                            Data = md,
                            Material = _manager.EngineRef.GetMaterialByName("defaultMat")
                        };

                        //Create and register locator mesh node
                        new_node = _manager.EngineRef.CreateMeshNode("Quad#1", nm);
                        entity_added = true;
                        Callbacks.Log(this, "Creating Quad Mesh Node", LogVerbosityLevel.INFO);
                    }

                    if (_clicked != null)
                    {
                        if (ImGuiCore.MenuItem("Duplicate"))
                        {
                            Callbacks.Log(this, "TODO: Duplicate the node", LogVerbosityLevel.INFO);
                        }
                    }


                    ImGuiCore.EndMenu();
                }

                if (ImGuiCore.MenuItem("Delete"))
                {
                    Console.WriteLine("Delete Node permanently");
                    _manager.EngineRef.DisposeSceneGraphNode(_clicked);
                    Console.WriteLine("Node deleted");
                }

                ImGuiCore.EndPopup();
            }


            if (ImGuiCore.BeginDragDropTarget())
            {
                ImGuiPayloadPtr payload = ImGuiCore.AcceptDragDropPayload("_TREENODE");

                ulong source_node_id = 0xFFFFFF;
                unsafe
                {
                    if (payload.NativePtr != null)
                    {
                        //Get data 
                        source_node_id = Marshal.PtrToStructure<ulong>(payload.Data);
                        //Console.WriteLine($"Add {source_node_id} to the children of {n.ID}");

                        //Get Source Node
                        SceneGraphNode g = (SceneGraphNode) _manager.EngineRef.GetEntityByID(source_node_id);
                        g.SetParent(n);
                        _manager.EngineRef.RequestEntityTransformUpdate(g);
                    }
                }

                ImGuiCore.EndDragDropTarget();
            }

            if (ImGuiCore.BeginDragDropSource())
            {
                unsafe 
                {
                    fixed (ulong* test = &n.ID)
                    {
                        
                        ImGuiCore.SetDragDropPayload("_TREENODE", new IntPtr(test), sizeof(ulong), ImGuiCond.Once);
                    }
                }
                
                ImGuiCore.EndDragDropSource();
            }


            if (n.IsOpen)
            {
                if (DrawChildren(n) > 0)
                    ImGuiCore.TreePop();
            }

        }
        

    }
}
