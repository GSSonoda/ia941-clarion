using System;
using System.Collections.Generic;
using System.Linq;

namespace ClarionApp.Model
{
    public class Memory
    {
        public IList<Thing> memoryJewel = new List<Thing>();
        public IList<Thing> memoryFood = new List<Thing>();
        private WSProxy worldServer;
        private String creatureName;

        public Memory(WSProxy worldServer, String creatureName)
        {
            this.worldServer = worldServer;
            this.creatureName = creatureName;
        }
        public void UpdateJewelAndFoodList(IList<Thing> currentSceneInWS3D)
        {
            UpdateMemoryJewel(currentSceneInWS3D);
            UpdateMemoryFood(currentSceneInWS3D);
            foreach (var thing in currentSceneInWS3D)
            {
                if (thing.CategoryId == 21 || thing.CategoryId == 2 || thing.CategoryId == 22)
                {
                    if (!memoryFood.Any(t => t.Name == thing.Name))
                    {
                        memoryFood.Add(thing);
                    }
                }
                else if (thing.CategoryId == 3)
                {
                    if (!memoryJewel.Any(t => t.Name == thing.Name))
                    {
                        memoryJewel.Add(thing);
                    }
                }
            }
        }
        private void UpdateMemoryJewel(IList<Thing> listThings)
        {
            //IList<Thing> listThings = this.worldServer.SendGetCreatureState(this.creatureName);
            if (listThings == null)
            {
                return;
            }
            for (int i = 0; i < memoryJewel.Count; i++)
            { 
                var newItem = listThings.FirstOrDefault(t => t.Name == memoryJewel[i].Name);
                if (newItem != null)
                {
                    memoryJewel[i] = newItem; // substitui o item por completo
                }
            }
        }

        private void UpdateMemoryFood(IList<Thing> listThings)
        {
            //IList<Thing> listThings = this.worldServer.SendGetCreatureState(this.creatureName);
            if (listThings == null)
            {
                return;
            }
            for (int i = 0; i < memoryFood.Count; i++)
            {
                var newItem = listThings.FirstOrDefault(t => t.Name == memoryFood[i].Name);
                if (newItem != null)
                {
                    memoryFood[i] = newItem; // substitui o item por completo
                }
            }
        }
        public IList<Thing> GetJewels()
        {
            return memoryJewel;
        }
        public IList<Thing> GetFoods()
        {
            return memoryFood;
        }

        public IList<Thing> GetFoodsJewels()
        {
            List<Thing> all = new List<Thing>();
            all.AddRange(memoryJewel);
            all.AddRange(memoryFood);
            return all;
        }
        public void Remove(Thing thing_to_remove)
        {
            if (thing_to_remove.CategoryId == 3)
            {
                memoryJewel.Remove(thing_to_remove);
            }
            if (thing_to_remove.CategoryId == 22 || thing_to_remove.CategoryId == 21 || thing_to_remove.CategoryId == 2)
            {
                memoryFood.Remove(thing_to_remove);
            }

        }
    }
}
