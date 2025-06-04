using System;
using System.Collections.Generic;
using System.Linq;

namespace ClarionApp.Model
{
    public class Deliver
    {
        private Leaflet leaflet;
        private WSProxy worldServer;
        private String creatureId;
        public Deliver(WSProxy worldServer, String creatureID)
        {
            this.worldServer = worldServer;
            this.creatureId = creatureID;
        }

        public void UpdateCompleteLeaflet(Leaflet l)
        {
            if (l != null && l.IsComplete())
            {
                this.leaflet = l;
            }
        }

        public void DoAction()
        {
            if (leaflet != null)
            {
                Console.WriteLine($"Entregando leaflet ID {leaflet.leafletID}...");
                worldServer.DeliverLeaflet(creatureId, Convert.ToInt32(leaflet.leafletID));
                leaflet = null;
            }

        }

        public Leaflet GetLeaflet()
        {
            return leaflet;
        }
    }
}
