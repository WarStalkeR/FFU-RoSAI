using System;
using Arcen.Universal;
using Arcen.HotM.Core;
using UnityEngine;

namespace Arcen.HotM.External
{
    public delegate bool ActionThatCanBeStopped<in T>( T obj );    
}
