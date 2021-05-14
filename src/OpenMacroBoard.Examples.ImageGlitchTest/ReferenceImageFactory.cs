﻿using OpenMacroBoard.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace OpenMacroBoard.Examples.ImageGlitchTest
{
    internal class ReferenceImageFactory
    {
        private const double TAU = Math.PI * 2;

        private int currentMode = 0;

        private readonly int imgSize;
        private readonly int keyCount;
        private readonly Stopwatch stopwatch = Stopwatch.StartNew();
        private readonly Random rnd = new Random();

        private Func<int, KeyBitmap> currentImageFactory;

        private readonly List<Func<int, KeyBitmap>> availableImageFactories
            = new List<Func<int, KeyBitmap>>();

        public ReferenceImageFactory(int imgSize, int keyCount)
        {
            this.imgSize = imgSize;
            this.keyCount = keyCount;

            availableImageFactories.AddRange(new Func<int, KeyBitmap>[]
            {
                GetBlank,
                Rainbow,
                GetStableFilledImage,
                GetStableLineImageVertical,
                GetChangingLineImageVertical,
                GetStableLineImageHorizontal,
                GetChangingLineImageHorizontal,
                GetChangingFilledImage
            });

            currentImageFactory = availableImageFactories[0];
        }

        public int ModeCount
            => availableImageFactories.Count;

        public int CurrentMode
        {
            get => currentMode;
            set
            {
                currentMode = Mod(value, ModeCount);
                currentImageFactory = availableImageFactories[currentMode];
            }
        }

        public KeyBitmap GetKeyBitmap(int keyId)
            => currentImageFactory(keyId);

        private static int Mod(int x, int m)
        {
            var r = x % m;
            return r < 0 ? r + m : r;
        }

        private static KeyBitmap GetBlank(int keyId)
            => KeyBitmap.Black;

        private int GetSawtoothTime(int msBase)
        {
            return (int)(stopwatch.ElapsedMilliseconds % msBase);
        }

        private static double LinearToSine(double linear, double min, double max)
            => (Math.Sin(linear * TAU) / 2.0 + 0.5) * (max - min) + min;

        private static double LinearToSine(double linear)
            => LinearToSine(linear, 0, 1);

        private KeyBitmap GetStableFilledImage(int key)
            => GetGrayImage((byte)DiscreteScale(0, 256, 0, keyCount, key, (g) => Math.Pow(g, 2.2)));

        private KeyBitmap GetStableLineImageVertical(int key)
            => GetVerticalStripeImage(DiscreteScale(0, imgSize, 0, keyCount, key));

        private KeyBitmap GetStableLineImageHorizontal(int key)
            => GetHorizontalStripeImage(DiscreteScale(0, imgSize, 0, keyCount, key));

        private KeyBitmap GetChangingLineImageVertical(int key)
            => GetVerticalStripeImage(rnd.Next(0, imgSize));

        private KeyBitmap GetChangingLineImageHorizontal(int key)
            => GetHorizontalStripeImage(rnd.Next(0, imgSize));

        private KeyBitmap GetChangingFilledImage(int key)
        {
            var limit = 3000;
            var t = GetSawtoothTime(limit);
            return GetGrayImage((byte)DiscreteScale(0, 255, 0, limit, t, LinearToSine));
        }

        private KeyBitmap GetVerticalStripeImage(int pos)
        {
            var raw = new byte[imgSize * imgSize * 3];

            for (var y = 0; y < imgSize; y++)
            {
                var p = (y * imgSize + pos) * 3;
                raw[p + 0] = 255;
                raw[p + 1] = 255;
                raw[p + 2] = 255;
            }

            return new KeyBitmap(imgSize, imgSize, raw);
        }

        private KeyBitmap Rainbow(int keyId)
        {
            var raw = new byte[imgSize * imgSize * 3];

            for (var y = 0; y < imgSize; y++)
            {
                for (var x = 0; x < imgSize; x++)
                {
                    var p = (y * imgSize + x) * 3;

                    var nX = (int)Math.Round((double)x / imgSize * 256, 0);
                    var nY = (int)Math.Round((double)y / imgSize * 256, 0);

                    if (nX > 255)
                    {
                        nX = 255;
                    }

                    if (nY > 255)
                    {
                        nY = 255;
                    }

                    var blue = (byte)nY;
                    var yellow = (byte)nX;
                    raw[p + 0] = blue;
                    raw[p + 1] = yellow;
                    raw[p + 2] = yellow;
                }
            }

            return new KeyBitmap(imgSize, imgSize, raw);
        }

        private KeyBitmap GetHorizontalStripeImage(int pos)
        {
            var raw = new byte[imgSize * imgSize * 3];
            for (var x = 0; x < imgSize; x++)
            {
                var p = (pos * imgSize + x) * 3;
                raw[p + 0] = 255;
                raw[p + 1] = 255;
                raw[p + 2] = 255;
            }
            return new KeyBitmap(imgSize, imgSize, raw);
        }

        private KeyBitmap GetGrayImage(byte b)
        {
            var raw = new byte[imgSize * imgSize * 3];

            for (var i = 0; i < raw.Length; i++)
            {
                raw[i] = b;
            }

            return new KeyBitmap(imgSize, imgSize, raw);
        }

        private static int DiscreteScale(
            int minOutInclusive,
            int maxOutExclusive,
            int minInInclusive,
            int maxInExclusive,
            int value,
            Func<double, double> transferFunction = null
        )
        {
            var maxOutInclusive = maxOutExclusive - 1;
            var maxInInclusive = maxInExclusive - 1;

            if (value < minInInclusive || value > maxInInclusive)
            {
                throw new ArgumentOutOfRangeException();
            }

            var diffOut = maxOutInclusive - minOutInclusive;
            var diffIn = maxInInclusive - minInInclusive;

            if (diffIn <= 0 || diffOut <= 0)
            {
                throw new ArgumentException();
            }

            var percent = (value - minInInclusive) / (double)diffIn;

            if (transferFunction != null)
            {
                percent = transferFunction(percent);
            }

            var outVal = (int)Math.Round(percent * diffOut, 0) + minOutInclusive;

            if (outVal <= minOutInclusive)
            {
                return minOutInclusive;
            }

            if (outVal >= maxOutInclusive)
            {
                return maxOutInclusive;
            }

            return outVal;
        }
    }
}
