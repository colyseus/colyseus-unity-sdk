using UnityEngine;
using System.Collections;

namespace Colyseus
{
    /// <summary>
    /// The base Networked Entity View
    /// </summary>
    public class ColyseusNetworkedEntityView : MonoBehaviour
    {
        /// <summary>
        /// The ID of the <see cref="ExampleNetworkedEntity"/> that belongs to this view.
        /// </summary>
        public string Id { get; protected set; }

    }
}