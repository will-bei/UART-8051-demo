using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
//Author: William Bei
//Published: October 15, 2016 Saturday
//
//Created with Microsoft Visual Studio 2015
namespace _8051_Control_GUI
{
    public partial class Form1 : Form
    {
        //Graphics variables
        private Graphics g;
        private Pen p;
        private Rectangle r;

        //Display, serial port variables
        private Line[][] pages;
        private Boolean[][] isShaded;

        //mouse position
        private int MouseX;
        private int MouseY;

        public Form1()
        { 
            /*
            *
            * 
            * LCD pixel organization: LCD is 64 by 128 pixels
            * Every 8 rows is divided into one page
            * Each page is divided into columns, each column containing a byte
            *   with each bit as one pixel
            * Writing LCD data must be done a byte at a time.
            * 
            */
            //initialize each pixel in the GUI
            pages = new Line[64][];
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i] = new Line[128];
            }
            for (int i = 0; i < pages.Length; i++)
            {
                for (int j = 0; j < pages[i].Length; j++)
                {
                    pages[i][j] = new Line(i, j);
                }
            }

            InitializeComponent();
            //disable altering windows form size
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            //add mouse listener
            this.MouseClick += mouseClick;
        }
        //Clear button: clears all pixels in LCD display
        private void button1_Click(object sender, EventArgs e)
        {
            textBox1.Text = "Clearing...";
            pages = new Line[64][];
            //Clear Laptop/Desktop Variables
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i] = new Line[128];
            }
            for (int i = 0; i < pages.Length; i++)
            {
                for (int j = 0; j < pages[i].Length; j++)
                {
                    pages[i][j] = new Line(i, j);
                }
            }
            isShaded = new Boolean[64][];
            for (int i = 0; i < isShaded.Length; i++)
            {
                isShaded[i] = new Boolean[128];
                for (int j = 0; j < isShaded[i].Length; j++)
                {
                    isShaded[i][j] = false;
                }
            }
            drawMethod();
            //Instruct 8051 to clear
            clearAll();
            textBox1.Text = "Cleared.";
        }
        //Open Serial Port for UART communication
        private void Initialize_Click(object sender, EventArgs e)
        {
            if (!serialPort1.IsOpen)
            {
                serialPort1.PortName = "COM3";
                serialPort1.Open();
                drawMethod();

                isShaded = new Boolean[64][];
                for (int i = 0; i < isShaded.Length; i++)
                {
                    isShaded[i] = new Boolean[128];
                    for (int j = 0; j < isShaded[i].Length; j++)
                    {
                        isShaded[i][j] = false;
                    }
                }

                clearAll();

            }
        }
        //creating the 64 x 128 LCD Display GUI 
        private void drawMethod()
        {
            g = CreateGraphics();
            p = new Pen(Brushes.Black);
            for (int i = 0; i < 128; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    r = new Rectangle(i * 5, j * 5, 5, 5);
                    g.FillRectangle(Brushes.White, r);
                    g.DrawRectangle(p, r);
                }
            }
        }
        //instructs 8051 to clear display
        private void clearAll()
        {
            //because 8051 organizes LCD in bytes (i.e. 8 pixels at a time)
            //essentially only 8 rows need to be cleared
            for (int i = 0; i < 64; i += 8)
            {
                for (int j = 0; j < 128; j++)
                {
                    Line temp = new Line(i, j);
                    byte[] toSend = temp.sendData();
                    serialPort1.Write(toSend, 0, 4);
                    serialPort1.Write(toSend, 0, 4);
                    serialPort1.Write(toSend, 0, 4);
                    serialPort1.Write(toSend, 0, 4);
                }
            }
        }
        //sync all pixels in the same column of the same page
        //this prevents the turning on/off of one pixel to turn on/off other pixels in
        //the same page and column
        private void syncpixels(int y, int x)
        {
            int bitnumber = y % 8;
            for (int i = 0; i <= 7; i++)
            {
                if (isShaded[(y - bitnumber) + i][x] && i != bitnumber)
                {
                    if (i == 0)
                    {
                        pages[y][x].select0();
                    }
                    if (i == 1)
                    {
                        pages[y][x].select1();
                    }
                    if (i == 2)
                    {
                        pages[y][x].select2();
                    }
                    if (i == 3)
                    {
                        pages[y][x].select3();
                    }
                    if (i == 4)
                    {
                        pages[y][x].select4();
                    }
                    if (i == 5)
                    {
                        pages[y][x].select5();
                    }
                    if (i == 6)
                    {
                        pages[y][x].select6();
                    }
                    if (i == 7)
                    {
                        pages[y][x].select7();
                    }
                }
            }
        }
        //when mouse is pressed, tests if it clicks a pixel on the GUI
        //if yes, see which pixel clicked
        //and send appropriate pixel data to 8051
        private void mouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && serialPort1.IsOpen)
            {
                MouseX = Cursor.Position.X;
                MouseY = Cursor.Position.Y;

                int windowTop = this.Top;
                int windowLeft = this.Left;

                //get the pixel that mouse is on
                int pixelX = 0;
                int pixelY = 0;
                int temp_X = 0;
                int temp_Y = 0;
                for (int i = 0; i < 128; i++)
                {
                    for (int j = 0; j < 64; j++)
                    {
                        Rectangle temp = new Rectangle(i * 5, j * 5, 5, 5);
                        temp_X = windowLeft + i * 5 + 7;
                        temp_Y = windowTop + j * 5 + 30;
                        if ((MouseX - temp_X) <= 5 && (MouseY - temp_Y) <= 5)
                        {
                            pixelX = i;
                            pixelY = j;

                            isShaded[pixelY][pixelX] = (!isShaded[pixelY][pixelX]);

                            //indicate on GUI that pixel is on/off
                            if (isShaded[pixelY][pixelX])
                            {
                                g.FillRectangle(Brushes.Blue, temp);
                                g.DrawRectangle(p, temp);
                            }
                            else
                            {
                                g.FillRectangle(Brushes.White, pixelX * 5, pixelY * 5, 5, 5);
                                g.DrawRectangle(p, pixelX * 5, pixelY * 5, 5, 5);
                            }



                            byte[] toSend = pages[pixelY][pixelX].sendData();
                            byte page = toSend[0];
                            byte col_h = toSend[1];
                            byte col_l = toSend[2];
                            byte data = toSend[3];

                            syncpixels(pixelY, pixelX);

                            toSend = pages[pixelY][pixelX].sendData();
                            page = toSend[0];
                            col_h = toSend[1];
                            col_l = toSend[2];
                            data = toSend[3];

                            //send data to serial port
                            int bitnumber = pixelY % 8;
                            if (bitnumber == 0)
                            {
                                pages[pixelY][pixelX].select0();
                            }
                            if (bitnumber == 1)
                            {
                                pages[pixelY][pixelX].select1();
                            }
                            if (bitnumber == 2)
                            {
                                pages[pixelY][pixelX].select2();
                            }
                            if (bitnumber == 3)
                            {
                                pages[pixelY][pixelX].select3();
                            }
                            if (bitnumber == 4)
                            {
                                pages[pixelY][pixelX].select4();
                            }
                            if (bitnumber == 5)
                            {
                                pages[pixelY][pixelX].select5();
                            }
                            if (bitnumber == 6)
                            {
                                pages[pixelY][pixelX].select6();
                            }
                            if (bitnumber == 7)
                            {
                                pages[pixelY][pixelX].select7();
                            }

                            toSend = pages[pixelY][pixelX].sendData();
                            page = toSend[0];
                            col_h = toSend[1];
                            col_l = toSend[2];
                            data = toSend[3];
                            serialPort1.Write(toSend, 0, 4);
                            serialPort1.Write(toSend, 0, 4);
                            serialPort1.Write(toSend, 0, 4);
                            serialPort1.Write(toSend, 0, 4);
                            serialPort1.Write(toSend, 0, 4);
                            serialPort1.Write(toSend, 0, 4);
                            serialPort1.Write(toSend, 0, 4);
                            textBox1.Text = "X: " + pixelX + " Y: " + pixelY;
                            return;
                        }
                    }
                }
            }
        }
        //Column Byte Class
        //A Line Object focuses on a certain pixel, but contains information of all pixels in the column
        //With Bitwise operators, syncs with other pixels and flips its own state when clicked
        public class Line
        {
            private int column;

            private byte page;
            private byte column_h;
            private byte column_l;
            private byte data;

            public const byte PAGESTARTER = 176;
            public const byte COL_H_STARTER = 16;
            public const byte COL_L_STARTER = 0;

            public Line(int point_Y, int col)
            {
                int page_number = point_Y / 8;

                //set page byte
                page = Convert.ToByte(page_number);
                page = Convert.ToByte(page | PAGESTARTER);

                byte ch = 0;
                byte cl = 0;
                string hexVal = col.ToString("X");
                if (hexVal.Length == 1)
                {
                    cl = Convert.ToByte(int.Parse(hexVal, System.Globalization.NumberStyles.HexNumber));
                }
                else
                {
                    char[] digits = hexVal.ToCharArray();
                    ch = Convert.ToByte(int.Parse(digits[0].ToString(), System.Globalization.NumberStyles.HexNumber));
                    cl = Convert.ToByte(int.Parse(digits[1].ToString(), System.Globalization.NumberStyles.HexNumber));
                }

                //set column high
                column = col;
                column_h = Convert.ToByte(ch);
                column_h = Convert.ToByte(column_h | COL_H_STARTER);

                //set column low
                column_l = Convert.ToByte(cl);
                column_l = Convert.ToByte(column_l | COL_L_STARTER);

                //initialize data byte
                data = 0;
            }
            public void select0()
            {
                byte j = 1;
                data = Convert.ToByte(data ^ j);
            }
            public void select1()
            {
                byte j = 2;
                data = Convert.ToByte(data ^ j);
            }
            public void select2()
            {
                byte j = 4;
                data = Convert.ToByte(data ^ j);
            }
            public void select3()
            {
                byte j = 8;
                data = Convert.ToByte(data ^ j);
            }
            public void select4()
            {
                byte j = 16;
                data = Convert.ToByte(data ^ j);
            }
            public void select5()
            {
                byte j = 32;
                data = Convert.ToByte(data ^ j);
            }
            public void select6()
            {
                byte j = 64;
                data = Convert.ToByte(data ^ j);
            }
            public void select7()
            {
                byte j = 128;
                data = Convert.ToByte(data ^ j);
            }
            public byte[] sendData()
            {
                byte[] bytes = new byte[4];
                bytes[0] = page;
                bytes[1] = column_h;
                bytes[2] = column_l;
                bytes[3] = data;

                return bytes;
            }
        }

        
    }
}
