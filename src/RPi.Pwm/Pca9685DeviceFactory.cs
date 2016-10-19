﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common.Logging;
using Raspberry.IO.Components.Controllers.Pca9685;
using Raspberry.IO.GeneralPurpose;
using Raspberry.IO.InterIntegratedCircuit;
using UnitsNet;

namespace RPi.Pwm
{
    public class Pca9685DeviceFactory : IDisposable
    {
        #region Fields

        private bool _disposed;
        public const ConnectorPin SdaPin = ConnectorPin.P1Pin03;
        public const ConnectorPin SclPin = ConnectorPin.P1Pin05;
        private static readonly ILog Log = LogManager.GetLogger< Pca9685DeviceFactory>();
        private I2cDriver _i2cDriver;

        #endregion

        #region Properties

        public bool IsConnected { get; set; }
        public Frequency PwmFrequency { get; set; }
        public int DeviceAddress { get; set; }

        #endregion
        
        #region Methods

        public IPwmDevice GetDevice(bool forceFakeDevice = false)
        {
            if (!forceFakeDevice && Environment.OSVersion.Platform == PlatformID.Unix)
            {
                return GetRealDevice();
            }
            else
            {
                return GetStubDevice();
            }
        }

        private IPwmDevice GetStubDevice()
        {
            Log.Info("This is not a Raspberry Pi. Giving fake PCA9685 PWM device");
            return new PwmDeviceStub();
        }

        private IPwmDevice GetRealDevice()
        {
            PwmFrequency = new Frequency(60);
            DeviceAddress = 0x40;

            try
            {
                Log.Info("Creating pins");
                var sdaPin = SdaPin.ToProcessor();
                var sclPin = SclPin.ToProcessor();

                Log.Info("Creating i2cDriver");
                _i2cDriver = new I2cDriver(sdaPin, sclPin);
            }
            catch (Exception e)
            {
                Log.Error("Failed to initialise i2c driver. Did you forget sudo?", e);
            }

            Log.Info("Creating real device...");
            var device = new Pca9685Connection(_i2cDriver.Connect(DeviceAddress));
            Log.Info("Setting frequency...");
            device.SetPwmUpdateRate(PwmFrequency); //                        # Set frequency to 60 Hz


            IsConnected = true;
            return device;
        }


        public void Dispose()
        {
            IsConnected = false;
            Dispose(true);

            // Call SupressFinalize in case a subclass implements a finalizer.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                // If you need thread safety, use a lock around these  
                // operations, as well as in your methods that use the resource. 
                if (disposing)
                {
                    // Free the necessary managed disposable objects. 
                    if (_i2cDriver != null)
                    {
                        _i2cDriver.Dispose();
                    }
                }

                _i2cDriver = null;
                _disposed = true;
            }
        }

        #endregion

    }
}
