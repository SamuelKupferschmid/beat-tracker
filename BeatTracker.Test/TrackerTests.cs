﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatTracker.Tracking;
using EasyAssertions;
using Xunit;

namespace BeatTracker.Test
{
    public class TrackerTests
    {
        [Fact]
        public void Test1()
        {
            new Tracker(null).ShouldNotBeNull();
        }
    }
}
