﻿using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using ImGuiNET;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using NativeWindow = OpenTK.NativeWindow;
using Vector4 = OpenTK.Vector4;

namespace BadgerEdit
{
    public class EditorWindow
    {
        private Editor editor;
        private NativeWindow _nativeWindow;
        private readonly int _scaleFactor;
        private GraphicsContext _graphicsContext;
        private IntPtr _textInputBuffer;
        private int _textInputBufferLength;
        private int s_fontTexture;
        private DateTime _previousFrameStartTime;
        private static double s_desiredFrameLength = 1f / 60.0f;
        private float _wheelPosition;
        private EditorRenderer Renderer;
        private bool _mainWindowOpened;
//        private readonly Vector4 clear_color = new Vector4(114f / 255f, 144f / 255f, 154f / 255f, 1.0f);
        private readonly Vector4 clear_color = new Vector4(0.2f, 0.2f, 0.2f, 1.0f);

        public unsafe EditorWindow()
        {
            int desiredWidth = 960, desiredHeight = 540;
            _nativeWindow = new OpenTK.NativeWindow(desiredWidth, desiredHeight, "BadgerEdit", GameWindowFlags.Default, OpenTK.Graphics.GraphicsMode.Default, DisplayDevice.Default);
            _scaleFactor = _nativeWindow.Width / desiredWidth;

            GraphicsContextFlags flags = GraphicsContextFlags.Default;
            _graphicsContext = new GraphicsContext(GraphicsMode.Default, _nativeWindow.WindowInfo, 3, 0, flags);
            _graphicsContext.MakeCurrent(_nativeWindow.WindowInfo);
            _graphicsContext.LoadAll();
            GL.ClearColor(Color.Black);
            _nativeWindow.Visible = true;
            _nativeWindow.X = _nativeWindow.X; // Work around OpenTK bug (?) on Ubuntu.

            //SquaredDisplay looks nice but is really flimsy
            //ImGui.GetIO().FontAtlas.AddFontFromFileTTF("fonts/SquaredDisplay.ttf", 13);
            ImGui.GetIO().FontAtlas.AddDefaultFont();
            
            SetOpenTKKeyMappings();

            _textInputBufferLength = 1024;
            _textInputBuffer = Marshal.AllocHGlobal(_textInputBufferLength);
            long* ptr = (long*)_textInputBuffer.ToPointer();
            for (int i = 0; i < 1024 / sizeof(long); i++)
            {
                ptr[i] = 0;
            }
            editor = new Editor();

            Renderer = new EditorRenderer(_nativeWindow, editor);
            
            _nativeWindow.KeyPress += NativeWindowOnKeyPress;
            _nativeWindow.KeyDown += NativeWindowOnKeyDown;
            _nativeWindow.KeyUp += NativeWindowOnKeyUp;
            CreateDeviceObjects();
        }
        
        private void NativeWindowOnKeyUp(object sender, KeyboardKeyEventArgs e)
        {
            ImGui.GetIO().KeysDown[(int)e.Key] = false;
            UpdateModifiers(e);
        }

        private void NativeWindowOnKeyDown(object sender, KeyboardKeyEventArgs e)
        {
            ImGui.GetIO().KeysDown[(int)e.Key] = true;
            UpdateModifiers(e);
        }
        private static void UpdateModifiers(KeyboardKeyEventArgs e)
        {
            IO io = ImGui.GetIO();
            io.AltPressed = e.Alt;
            io.CtrlPressed = e.Control;
            io.ShiftPressed = e.Shift;
        }


        private void NativeWindowOnKeyPress(object sender, KeyPressEventArgs keyPressEventArgs)
        {
            IO io = ImGui.GetIO();
            if (io.WantCaptureKeyboard)
            {
                ImGui.AddInputCharacter(keyPressEventArgs.KeyChar);
            }
            else
            {
                if (io.CtrlPressed || io.AltPressed)
                    return;
                editor.CharKey(keyPressEventArgs.KeyChar);
            }
        }

        private static unsafe void SetOpenTKKeyMappings()
        {
            IO io = ImGui.GetIO();
            io.KeyMap[GuiKey.Tab] = (int)Key.Tab;
            io.KeyMap[GuiKey.LeftArrow] = (int)Key.Left;
            io.KeyMap[GuiKey.RightArrow] = (int)Key.Right;
            io.KeyMap[GuiKey.UpArrow] = (int)Key.Up;
            io.KeyMap[GuiKey.DownArrow] = (int)Key.Down;
            io.KeyMap[GuiKey.PageUp] = (int)Key.PageUp;
            io.KeyMap[GuiKey.PageDown] = (int)Key.PageDown;
            io.KeyMap[GuiKey.Home] = (int)Key.Home;
            io.KeyMap[GuiKey.End] = (int)Key.End;
            io.KeyMap[GuiKey.Delete] = (int)Key.Delete;
            io.KeyMap[GuiKey.Backspace] = (int)Key.BackSpace;
            io.KeyMap[GuiKey.Enter] = (int)Key.Enter;
            io.KeyMap[GuiKey.Escape] = (int)Key.Escape;
            io.KeyMap[GuiKey.A] = (int)Key.A;
            io.KeyMap[GuiKey.C] = (int)Key.C;
            io.KeyMap[GuiKey.V] = (int)Key.V;
            io.KeyMap[GuiKey.X] = (int)Key.X;
            io.KeyMap[GuiKey.Y] = (int)Key.Y;
            io.KeyMap[GuiKey.Z] = (int)Key.Z;
        }

        public void RunWindowLoop()
        {
            var io = ImGui.GetIO();
            _nativeWindow.Visible = true;
            while (_nativeWindow.Exists)
            {
                _previousFrameStartTime = DateTime.UtcNow;

                RenderFrame();

                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.Enter)))
                {
                    editor.CharKey('\n');
                }
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.Delete)))
                {
                    editor.Delete();
                }
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.Tab)))
                {
                    editor.CharKey('\t');
                }
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.Backspace)))
                {
                    editor.Backspace();
                }
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.UpArrow)))
                {
                    editor.Move(MoveDirective.Up, io.ShiftPressed);
                }
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.DownArrow)))
                {
                    editor.Move(MoveDirective.Down, io.ShiftPressed);
                }
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.LeftArrow)))
                {
                    editor.Move(io.CtrlPressed ? MoveDirective.StartOfWord : MoveDirective.Left, io.ShiftPressed);
                }
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.RightArrow)))
                {
                    editor.Move(io.CtrlPressed ? MoveDirective.EndOfWord : MoveDirective.Right, io.ShiftPressed);
                }

                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.Home)))
                {
                     editor.Move(io.CtrlPressed ? MoveDirective.StartOfDocument : MoveDirective.Home, io.ShiftPressed);
                }

                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.End)))
                {
                    editor.Move(io.CtrlPressed ? MoveDirective.EndOfDocument : MoveDirective.End, io.ShiftPressed);
                }

                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.A)) && io.CtrlPressed)
                {
                    editor.SelectAll();
                }
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.C)) && io.CtrlPressed)
                {
                    editor.Copy();
                }
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.X)) && io.CtrlPressed)
                {
                    editor.Cut();
                }
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.V)) && io.CtrlPressed)
                {
                    editor.Paste();
                }
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.PageUp)))
                {
                    editor.Move(MoveDirective.PageUp, io.ShiftPressed, Renderer.FirstVisibleLine, Renderer.LastVisibleLine);
                }
                if (ImGui.IsKeyPressed(ImGui.GetKeyIndex(GuiKey.PageDown)))
                {
                    editor.Move(MoveDirective.PageDown, io.ShiftPressed, Renderer.FirstVisibleLine, Renderer.LastVisibleLine);
                }

                _nativeWindow.ProcessEvents();

                DateTime afterFrameTime = DateTime.UtcNow;
                double elapsed = (afterFrameTime - _previousFrameStartTime).TotalSeconds;
                double sleepTime = s_desiredFrameLength - elapsed;
                if (sleepTime > 0.0)
                {
                    DateTime finishTime = afterFrameTime + TimeSpan.FromSeconds(sleepTime);
                    while (DateTime.UtcNow < finishTime)
                    {
                        Thread.Sleep(0);
                    }
                }
            }
        }

        private unsafe void RenderFrame()
        {
            IO io = ImGui.GetIO();
            io.DisplaySize = new System.Numerics.Vector2(_nativeWindow.Width, _nativeWindow.Height);
            io.DisplayFramebufferScale = new System.Numerics.Vector2(_scaleFactor);
            io.DeltaTime = (1f / 60f);

            UpdateImGuiInput(io);

            ImGui.NewFrame();

            SubmitImGuiStuff();

            ImGui.Render();

            DrawData* data = ImGui.GetDrawData();
            RenderImDrawData(data);
        }
        
        private void SubmitImGuiStuff()
        {
            ImGui.GetStyle().WindowRounding = 0;

            ImGui.SetNextWindowSize(new System.Numerics.Vector2(_nativeWindow.Width - 20, _nativeWindow.Height - 30), Condition.Appearing);
            //ImGui.SetNextWindowPos(ImGui.GetIO().DisplaySize, Condition.Always, new System.Numerics.Vector2(1f));

            Renderer.Render();
            
            if (ImGui.GetIO().AltPressed && ImGui.GetIO().KeysDown[(int)Key.F4])
            {
                _nativeWindow.Close();
            }
        }
        private void UpdateImGuiInput(IO io)
        {
            MouseState cursorState = Mouse.GetCursorState();
            MouseState mouseState = Mouse.GetState();

            if (_nativeWindow.Focused)
            {
                Point windowPoint = _nativeWindow.PointToClient(new Point(cursorState.X, cursorState.Y));
                io.MousePosition = new System.Numerics.Vector2(windowPoint.X / io.DisplayFramebufferScale.X, windowPoint.Y / io.DisplayFramebufferScale.Y);
            }
            else
            {
                io.MousePosition = new System.Numerics.Vector2(-1f, -1f);
            }

            io.MouseDown[0] = mouseState.LeftButton == ButtonState.Pressed;
            io.MouseDown[1] = mouseState.RightButton == ButtonState.Pressed;
            io.MouseDown[2] = mouseState.MiddleButton == ButtonState.Pressed;

            float newWheelPos = mouseState.WheelPrecise;
            float delta = newWheelPos - _wheelPosition;
            _wheelPosition = newWheelPos;
            io.MouseWheel = delta;
        }

        private unsafe void RenderImDrawData(DrawData* draw_data)
        {
            // Rendering
            int display_w, display_h;
            display_w = _nativeWindow.Width;
            display_h = _nativeWindow.Height;
            
            GL.Viewport(0, 0, display_w, display_h);
            GL.ClearColor(clear_color.X, clear_color.Y, clear_color.Z, clear_color.W);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // We are using the OpenGL fixed pipeline to make the example code simpler to read!
            // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, vertex/texcoord/color pointers.
            int last_texture;
            GL.GetInteger(GetPName.TextureBinding2D, out last_texture);
            GL.PushAttrib(AttribMask.EnableBit | AttribMask.ColorBufferBit | AttribMask.TransformBit);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
            GL.Enable(EnableCap.ScissorTest);
            GL.EnableClientState(ArrayCap.VertexArray);
            GL.EnableClientState(ArrayCap.TextureCoordArray);
            GL.EnableClientState(ArrayCap.ColorArray);
            GL.Enable(EnableCap.Texture2D);

            GL.UseProgram(0);

            // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
            IO io = ImGui.GetIO();
            ImGui.ScaleClipRects(draw_data, io.DisplayFramebufferScale);

            // Setup orthographic projection matrix
            GL.MatrixMode(MatrixMode.Projection);
            GL.PushMatrix();
            GL.LoadIdentity();
            GL.Ortho(
                0.0f,
                io.DisplaySize.X / io.DisplayFramebufferScale.X,
                io.DisplaySize.Y / io.DisplayFramebufferScale.Y,
                0.0f,
                -1.0f,
                1.0f);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PushMatrix();
            GL.LoadIdentity();

            //todo : is this where i render the background image?

            // Render command lists
            for (int n = 0; n < draw_data->CmdListsCount; n++)
            {
                NativeDrawList* cmd_list = draw_data->CmdLists[n];
                byte* vtx_buffer = (byte*)cmd_list->VtxBuffer.Data;
                ushort* idx_buffer = (ushort*)cmd_list->IdxBuffer.Data;

                GL.VertexPointer(2, VertexPointerType.Float, sizeof(DrawVert), new IntPtr(vtx_buffer + DrawVert.PosOffset));
                GL.TexCoordPointer(2, TexCoordPointerType.Float, sizeof(DrawVert), new IntPtr(vtx_buffer + DrawVert.UVOffset));
                GL.ColorPointer(4, ColorPointerType.UnsignedByte, sizeof(DrawVert), new IntPtr(vtx_buffer + DrawVert.ColOffset));

                for (int cmd_i = 0; cmd_i < cmd_list->CmdBuffer.Size; cmd_i++)
                {
                    DrawCmd* pcmd = &((DrawCmd*)cmd_list->CmdBuffer.Data)[cmd_i];
                    if (pcmd->UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    GL.BindTexture(TextureTarget.Texture2D, pcmd->TextureId.ToInt32());
                    GL.Scissor(
                        (int)pcmd->ClipRect.X,
                        (int)(io.DisplaySize.Y - pcmd->ClipRect.W),
                        (int)(pcmd->ClipRect.Z - pcmd->ClipRect.X),
                        (int)(pcmd->ClipRect.W - pcmd->ClipRect.Y));
                    try
                    {
                        GL.DrawElements(PrimitiveType.Triangles, (int) pcmd->ElemCount, DrawElementsType.UnsignedShort,
                            new IntPtr(idx_buffer));
                    }
                    catch (Exception e)
                    {
                        Console.Out.WriteLine("DRAW FAIL " + e.Message);
                    }
                    idx_buffer += pcmd->ElemCount;
                }
            }

            // Restore modified state
            GL.DisableClientState(ArrayCap.ColorArray);
            GL.DisableClientState(ArrayCap.TextureCoordArray);
            GL.DisableClientState(ArrayCap.VertexArray);
            GL.BindTexture(TextureTarget.Texture2D, last_texture);
            GL.MatrixMode(MatrixMode.Modelview);
            GL.PopMatrix();
            GL.MatrixMode(MatrixMode.Projection);
            GL.PopMatrix();
            GL.PopAttrib();

            _graphicsContext.SwapBuffers();
        }



        private unsafe void CreateDeviceObjects()
        {
            IO io = ImGui.GetIO();

            // Build texture atlas
            FontTextureData texData = io.FontAtlas.GetTexDataAsAlpha8();

            // Create OpenGL texture
            s_fontTexture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, s_fontTexture);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)All.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)All.Linear);
            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Alpha,
                texData.Width,
                texData.Height,
                0,
                PixelFormat.Alpha,
                PixelType.UnsignedByte,
                new IntPtr(texData.Pixels));

            // Store the texture identifier in the ImFontAtlas substructure.
            io.FontAtlas.SetTexID(s_fontTexture);

            // Cleanup (don't clear the input data if you want to append new fonts later)
            //io.Fonts->ClearInputData();
            io.FontAtlas.ClearTexData();
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }
    }
}
