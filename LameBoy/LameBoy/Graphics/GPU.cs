﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SDL2;

namespace LameBoy.Graphics
{
    public class GPU
    {
        SDLThread sdlt;
        Cart cart;
        byte[,] frame;
        List<byte[,]> tiles;
        public bool drawing = false;
        public bool IsRunning {get; private set;}
        byte tileChecksum = 0;
        byte bgChecksum = 0;

        /// <summary>
        /// Starts the GPU with no associated runtime.
        /// </summary>
        public GPU()
        {
            frame = new byte[160, 144];
            tiles = new List<byte[,]>();
            Palette.SetColors(new byte[] { 0xE0, 0xF8, 0xD0 }, new byte[] { 0x88, 0xC0, 0x70 }, new byte[] { 0x34, 0x68, 0x56 }, new byte[] { 0x08, 0x18, 0x20 });
            IsRunning = true;
        }

        /// <summary>
        /// Starts the GPU along with an SDL runtime.
        /// </summary>
        /// <param name="Handle">The main form's handle</param>
        /// <param name="pgHandle">The graphics panel's handle</param>
        public GPU(IntPtr Handle, IntPtr pgHandle) : this()
        {
            sdlt = new SDLThread(Handle, pgHandle, this);
            Thread sdlThread = new Thread(new ThreadStart(sdlt.Render));
            sdlThread.Start();
        }

        public void Shutdown()
        {
            sdlt.Terminate();
        }

        public byte GetYCounter()
        {
            if(cart != null)
            {
                return cart.Read8(0xFF44);
            }
            return 0;
        }

        //xxx code smells start here
        public void SetScale(int scale)
        {
            sdlt.rt.scale = scale;
        }

        public void SetCPUExecutionState(State state)
        {
            sdlt.rt.CPUexecuting = state;
        }

        public void SetCart(Cart NewCart)
        {
            cart = NewCart;
        }
        //end code smells

        public void UpdateYCounter(byte count)
        {
            if(cart != null)
            {
                cart.Write8(0xFF44, count);
            }
        }
        
        private byte[,] DecodeTile(byte[] tile)
        {
            BitArray tileBits = new BitArray(tile);
            byte[,] lines = new byte[8, 8];
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    int highBitPos = ((7 - x) + 8 + (y * 16));
                    int lowBitPos = (7 - x) + (y * 16);
                    byte high = (byte)(Convert.ToByte(tileBits.Get(highBitPos)) << 1);
                    byte low = Convert.ToByte(tileBits.Get(lowBitPos));
                    byte color = (byte)(high | low);
                    lines[y, x] = color;
                }
            }
            return lines;
        }

        private void PushFrame()
        {
            sdlt.rt.SetPixels(frame);
        }

        private void DrawTile(byte[,] tile, int xCoord, int yCoord)
        {
            if (xCoord > 159 || yCoord > 143)
                return;

            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    frame[x + xCoord, y + yCoord] = tile[y, x];
                }
            }
        }

        private bool CheckBGSum()
        {
            byte check = 0;
            for(int i = 0; i < 0x20; i++)
            {
                check += cart.Read8(0x9800 + i);
            }
            return check != bgChecksum;
        }

        private bool CheckTileSum()
        {
            byte check = 0;
            for(int i = 0; i < 0xF0; i += 0x10)
            {
                check += cart.Read8(0x8000 + (i * 0x10));
            }
            return check != tileChecksum;
        }

        public void RenderScene()
        {
            while (IsRunning)
            {
                drawing = true;
                if (cart == null)
                {
                    //Splash screen
                    byte[,] L = DecodeTile(new byte[] { 0x00, 0x00, 0x60, 0x60, 0x60, 0x60, 0x60, 0x60, 0x60, 0x60, 0x60, 0x60, 0x7E, 0x7E, 0x00, 0x00 });
                    byte[,] a = DecodeTile(new byte[] { 0x00, 0x00, 0x3C, 0x3C, 0x4E, 0x4E, 0x4E, 0x4E, 0x7E, 0x7E, 0x4E, 0x4E, 0x4E, 0x4E, 0x00, 0x00 });
                    byte[,] m = DecodeTile(new byte[] { 0x00, 0x00, 0x46, 0x46, 0x6E, 0x6E, 0x7E, 0x7E, 0x56, 0x56, 0x46, 0x46, 0x46, 0x46, 0x00, 0x00 });
                    byte[,] e = DecodeTile(new byte[] { 0x00, 0x00, 0x7E, 0x7E, 0x60, 0x60, 0x7C, 0x7C, 0x60, 0x60, 0x60, 0x60, 0x7E, 0x7E, 0x00, 0x00 });
                    byte[,] B = DecodeTile(new byte[] { 0x00, 0x00, 0x7C, 0x7C, 0x66, 0x66, 0x7C, 0x7C, 0x66, 0x66, 0x66, 0x66, 0x7C, 0x7C, 0x00, 0x00 });
                    byte[,] o = DecodeTile(new byte[] { 0x00, 0x00, 0x3C, 0x3C, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x3C, 0x3C, 0x00, 0x00 });
                    byte[,] y = DecodeTile(new byte[] { 0x00, 0x00, 0x66, 0x66, 0x66, 0x66, 0x3C, 0x3C, 0x18, 0x18, 0x18, 0x18, 0x18, 0x18, 0x00, 0x00 });
                    List<byte[,]> letters = new List<byte[,]>();
                    letters.Add(L);
                    letters.Add(a);
                    letters.Add(m);
                    letters.Add(e);
                    letters.Add(B);
                    letters.Add(o);
                    letters.Add(y);
                    for (int i = 0; i < 7; i++)
                    {
                        DrawTile(letters.ElementAt(i), (i + 6) * 8, 30);
                    }
                    PushFrame();
                    drawing = false;
                }
                else
                {
                    //Render the scene here
                    //Once CPU is implemented, this will copy each tile object from
                    //VRAM into a byte array, and each will be rendered according to
                    //data stored in OAM

                    //Copy tiles
                    if (CheckTileSum())
                    {
                        tiles = new List<byte[,]>();
                        tileChecksum = 0;
                        for (int n = 0; n < 0xFF; n++)
                        {
                            byte[] tile = new byte[16];
                            for (int i = 0; i < 16; i++)
                            {
                                //4329 = 1
                                //378F = square block
                                tile[i] = cart.Read8(0x8000 + (n * 0x10) + i);
                            }
                            if (n % 0x10 == 0)
                            {
                                tileChecksum += cart.Read8(0x8000 + (n * 0x10));
                            }
                            tiles.Add(DecodeTile(tile));
                        }
                    }

                    //Render background
                    if (CheckBGSum())
                    {
                        for (int i = 0; i < 0x20; i++)
                        {
                            bgChecksum += cart.Read8(0x9800 + i);
                        }
                        for (int y = 0; y < 0x20; y++)
                        {
                            for (int x = 0; x < 0x20; x++)
                            {
                                int val = (int) cart.Read8(0x9800 + x + (y * 0x20));
                                if (val > tiles.Capacity)
                                    continue;
                                if (val == 0)
                                    continue;
                                byte[,] tile = tiles.ElementAt(val);
                                if (x < 0x14 && y < 0x14)
                                    DrawTile(tile, x * 8, y * 8);
                            }
                        }
                    }
                    
                    //Copy OAM
                    byte[,] OAM = new byte[40, 4];
                    for (int i = 0; i <= 0x9C; i += 4)
                    {
                        for(int n = 0; n < 4; n++)
                        {
                            OAM[(int)(i / 4), n] = cart.Read8(0xFE00 + i + n);
                        }
                    }

                    //Draw OAM
                    for (int i = 0; i < 40; i++)
                    {
                        byte OAMY = OAM[i, 0];
                        byte OAMX = OAM[i, 1];
                        byte OAMTile = OAM[i, 2];
                        byte OAMFlags = OAM[i, 3];
                        if (OAMX == 0 || OAMY == 0)
                            continue;
                        if (OAMX > 8 || OAMY > 16)
                            continue;
                        if((OAMFlags & 0x80) == 0x80)
                        {
                            DrawTile(tiles.ElementAt(OAMTile), OAMX - 8, OAMY - 16);
                        }
                    }
                    PushFrame();
                    drawing = false;
                    for (byte y = 0; y < 155; y++)
                    {
                        //Fix this somehow
                        while (sdlt.rt.CPUexecuting != State.Running) { };
                        UpdateYCounter(y);
                    }
                }
            }
        }

        public void Terminate()
        {
            sdlt.Terminate();
            IsRunning = false;
        }
    }
}
