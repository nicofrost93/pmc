﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace VoxData
{
    public partial class PolygonWaveFormControl : UserControl
    {
        public static readonly DependencyProperty SampleAggregatorProperty = DependencyProperty.Register(
          "SampleAggregator", typeof(SampleAggregator), typeof(PolygonWaveFormControl), new PropertyMetadata(null, OnSampleAggregatorChanged));

        public SampleAggregator SampleAggregator
        {
            get { return (SampleAggregator)this.GetValue(SampleAggregatorProperty); }
            set { this.SetValue(SampleAggregatorProperty, value); }
        }

        private static void OnSampleAggregatorChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            PolygonWaveFormControl control = (PolygonWaveFormControl)sender;
            control.Subscribe();
        }

        public void Subscribe()
        {
            SampleAggregator.MaximumCalculated += SampleAggregator_MaximumCalculated;
        }

        void SampleAggregator_MaximumCalculated(object sender, MaxSampleEventArgs e)
        {
            if (this.IsEnabled)
            {
                this.AddValue(e.MaxSample, e.MinSample);
            }
        }

        private int renderPosition;
        private double yTranslate = 40;
        private double yScale = 40;
        private double xScale = 2;
        private int blankZone = 10;

        Polygon waveForm = new Polygon();

        public PolygonWaveFormControl()
        {
            this.SizeChanged += OnSizeChanged;
            InitializeComponent();
            waveForm.Stroke = this.Foreground;
            waveForm.StrokeThickness = 1;
            waveForm.Fill = new SolidColorBrush(Colors.Bisque);
            mainCanvas.Children.Add(waveForm);
        }

        void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            // We will remove everything as we are going to rescale vertically
            renderPosition = 0;
            ClearAllPoints();

            this.yTranslate = this.ActualHeight / 2;
            this.yScale = this.ActualHeight / 2;
        }

        private void ClearAllPoints()
        {
            waveForm.Points.Clear();
        }

        private int Points
        {
            get { return waveForm.Points.Count / 2; }
        }

        public void AddValue(float maxValue, float minValue)
        {
            int visiblePixels = (int)(ActualWidth / xScale);
            if (visiblePixels > 0)
            {
                CreatePoint(maxValue, minValue);

                if (renderPosition > visiblePixels)
                {
                    renderPosition = 0;
                }
                int erasePosition = (renderPosition + blankZone) % visiblePixels;
                if (erasePosition < Points)
                {
                    double yPos = SampleToYPosition(0);
                    waveForm.Points[erasePosition] = new Point(erasePosition * xScale, yPos);
                    waveForm.Points[BottomPointIndex(erasePosition)] = new Point(erasePosition * xScale, yPos);
                }
            }
        }

        private int BottomPointIndex(int position)
        {
            return waveForm.Points.Count - position - 1;
        }

        private double SampleToYPosition(float value)
        {
            return yTranslate + value * yScale;
        }

        private void CreatePoint(float topValue, float bottomValue)
        {
            double topYPos = SampleToYPosition(topValue);
            double bottomYPos = SampleToYPosition(bottomValue);
            double xPos = renderPosition * xScale;
            if (renderPosition >= Points)
            {
                int insertPos = Points;
                waveForm.Points.Insert(insertPos, new Point(xPos, topYPos));
                waveForm.Points.Insert(insertPos + 1, new Point(xPos, bottomYPos));
            }
            else
            {
                waveForm.Points[renderPosition] = new Point(xPos, topYPos);
                waveForm.Points[BottomPointIndex(renderPosition)] = new Point(xPos, bottomYPos);
            }
            renderPosition++;
        }

        /// <summary>
        /// Clears the waveform and repositions on the left
        /// </summary>
        public void Reset()
        {
            renderPosition = 0;
            ClearAllPoints();
        }
    }
}
