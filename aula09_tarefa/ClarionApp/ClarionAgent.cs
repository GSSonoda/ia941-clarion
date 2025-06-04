using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Clarion;
using Clarion.Framework;
using Clarion.Framework.Core;
using Clarion.Framework.Templates;
using ClarionApp.Model;
using ClarionApp;
using System.Threading;
using Gtk;

namespace ClarionApp
{
    /// <summary>
    /// Public enum that represents all possibilities of agent actions
    /// </summary>
    public enum CreatureActions
    {
        DO_NOTHING,
        ROTATE_CLOCKWISE,
        GO_AHEAD,
        MOVE,
        GET,
        DELIVER
    }

    public class ClarionAgent
    {
        #region Constants
        /// <summary>
        /// Constant that represents the Visual Sensor
        /// </summary>
        private String SENSOR_VISUAL_DIMENSION = "VisualSensor";
        private String MEMORY = "Memory";
        private String INTERNAL_SENSOR = "InternalSensor";
        /// <summary>
        /// Constant that represents that there is at least one wall ahead
        /// </summary>
        private String DIMENSION_WALL_AHEAD = "WallAhead";  
		double prad = 0;
        #endregion
        Random random = new Random();
        #region Properties
        public MindViewer mind;
		String creatureId = String.Empty;
		String creatureName = String.Empty;
        public Memory memory;
        private Deliver deliver;
        #region Simulation
        /// <summary>
        /// If this value is greater than zero, the agent will have a finite number of cognitive cycle. Otherwise, it will have infinite cycles.
        /// </summary>
        public double MaxNumberOfCognitiveCycles = -1;
        /// <summary>
        /// Current cognitive cycle number
        /// </summary>
        private double CurrentCognitiveCycle = 0;
        /// <summary>
        /// Time between cognitive cycle in miliseconds
        /// </summary>
        public Int32 TimeBetweenCognitiveCycles = 0;
        /// <summary>
        /// A thread Class that will handle the simulation process
        /// </summary>
        private Thread runThread;
        #endregion

        #region Agent
		private WSProxy worldServer;
        /// <summary>
        /// The agent 
        /// </summary>
        private Clarion.Framework.Agent CurrentAgent;
        #endregion

        #region Perception Input
        /// <summary>
        /// Perception input to indicates a wall ahead
        /// </summary>
		private DimensionValuePair inputWallAhead;
        private DimensionValuePair inputHasFoodInMemory;
        private DimensionValuePair inputHasJewelInMemory;
        private DimensionValuePair inputLowFuel;
        private DimensionValuePair inputFoodAhead;
        private DimensionValuePair inputJewelAhead;
        private DimensionValuePair inputCanCompleteLeaflet;
        #endregion

        #region Action Output
        /// <summary>
        /// Output action that makes the agent to rotate clockwise
        /// </summary>
		private ExternalActionChunk outputRotateClockwise;
        /// <summary>
        /// Output action that makes the agent go ahead
        /// </summary>
		private ExternalActionChunk outputGoAhead;
        private ExternalActionChunk outputMove;
        private ExternalActionChunk outputGet;
        private ExternalActionChunk outputDeliver;
        #endregion

        #endregion

        #region Constructor
        public ClarionAgent(WSProxy nws, String creature_ID, String creature_Name)
        {
			worldServer = nws;
			// Initialize the agent
            CurrentAgent = World.NewAgent("Current Agent");
			mind = new MindViewer();
			mind.Show ();
			creatureId = creature_ID;
			creatureName = creature_Name;

            memory = new Memory(worldServer, creatureName);
            deliver = new Deliver(worldServer, creatureId);

            // Initialize Input Information
            inputWallAhead = World.NewDimensionValuePair(SENSOR_VISUAL_DIMENSION, DIMENSION_WALL_AHEAD);
            inputHasFoodInMemory = World.NewDimensionValuePair(MEMORY, "HasFoodInMemory");
            inputHasJewelInMemory = World.NewDimensionValuePair(MEMORY, "HasJewelInMemory");
            inputCanCompleteLeaflet = World.NewDimensionValuePair(MEMORY, "CanCompleteLeaflet");
            inputLowFuel = World.NewDimensionValuePair(INTERNAL_SENSOR, "LowFuel");
            inputFoodAhead = World.NewDimensionValuePair(SENSOR_VISUAL_DIMENSION, "FoodAhead");
            inputJewelAhead = World.NewDimensionValuePair(SENSOR_VISUAL_DIMENSION, "JewelAhead");

            // Initialize Output actions
            outputRotateClockwise = World.NewExternalActionChunk(CreatureActions.ROTATE_CLOCKWISE.ToString());
            outputGoAhead = World.NewExternalActionChunk(CreatureActions.GO_AHEAD.ToString());
            outputMove = World.NewExternalActionChunk(CreatureActions.MOVE.ToString());
            outputGet = World.NewExternalActionChunk(CreatureActions.GET.ToString());
            outputDeliver = World.NewExternalActionChunk(CreatureActions.DELIVER.ToString());

            //Create thread to simulation
            runThread = new Thread(CognitiveCycle);
			Console.WriteLine("Agent started");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Run the Simulation in World Server 3d Environment
        /// </summary>
        public void Run()
        {                
			Console.WriteLine ("Running ...");
            // Setup Agent to run
            if (runThread != null && !runThread.IsAlive)
            {
                SetupAgentInfraStructure();
				// Start Simulation Thread                
                runThread.Start(null);
            }
        }

        /// <summary>
        /// Abort the current Simulation
        /// </summary>
        /// <param name="deleteAgent">If true beyond abort the current simulation it will die the agent.</param>
        public void Abort(Boolean deleteAgent)
        {   Console.WriteLine ("Aborting ...");
            if (runThread != null && runThread.IsAlive)
            {
                runThread.Abort();
            }

            if (CurrentAgent != null && deleteAgent)
            {
                CurrentAgent.Die();
            }
        }

		IList<Thing> processSensoryInformation()
		{
			IList<Thing> response = null;

			if (worldServer != null && worldServer.IsConnected)
			{
				response = worldServer.SendGetCreatureState(creatureName);
				prad = (Math.PI / 180) * response.First().Pitch;
				while (prad > Math.PI) prad -= 2 * Math.PI;
				while (prad < - Math.PI) prad += 2 * Math.PI;
				Sack s = worldServer.SendGetSack("0");
				mind.setBag(s);
			}

			return response;
		}

		void processSelectedAction(CreatureActions externalAction)
		{   Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
			if (worldServer != null && worldServer.IsConnected)
			{
				switch (externalAction)
				{
				case CreatureActions.DO_NOTHING:
                    break;
				case CreatureActions.ROTATE_CLOCKWISE:
					worldServer.SendSetAngle(creatureId, 2, -2, 2);
					break;
				case CreatureActions.GO_AHEAD:
					worldServer.SendSetAngle(creatureId, 1, 1, prad);
					break;
                case CreatureActions.MOVE:
                    Move move = new Move(worldServer, memory.GetJewels(), creatureId);
                    move.DoAction();
                    break;
                case CreatureActions.GET:
                    Get get = new Get(worldServer, memory.GetJewels(), creatureId);
                    get.DoAction();
                    memory.Remove(get.GetThing());
                    break;
                case CreatureActions.DELIVER:
                    deliver.DoAction();
                    break;
				default:
					break;
				}
			}
		}

        #endregion

        #region Setup Agent Methods
        /// <summary>
        /// Setup agent infra structure (ACS, NACS, MS and MCS)
        /// </summary>
        private void SetupAgentInfraStructure()
        {
            // Setup the ACS Subsystem
            SetupACS();                    
        }

        private void SetupMS()
        {            
            //RichDrive
        }

        /// <summary>
        /// Setup the ACS subsystem
        /// </summary>
        private void SetupACS()
        {
            // Create Rule to avoid collision with wall
            SupportCalculator avoidCollisionWallSupportCalculator = FixedRuleToAvoidCollisionWall;
            FixedRule ruleAvoidCollisionWall = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, outputRotateClockwise, avoidCollisionWallSupportCalculator);

            // Commit this rule to Agent (in the ACS)
            CurrentAgent.Commit(ruleAvoidCollisionWall);

            // Create Colission To Go Ahead
            SupportCalculator goAheadSupportCalculator = FixedRuleToGoAhead;
            // FixedRule ruleGoAhead = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, outputGoAhead, goAheadSupportCalculator);
            FixedRule ruleGoAhead = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, outputGoAhead, goAheadSupportCalculator);

            // Commit this rule to Agent (in the ACS)
            CurrentAgent.Commit(ruleGoAhead);

            SupportCalculator moveToFoodSupport = FixedRuleToMoveToFood;
            FixedRule ruleMoveToFood = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, outputMove, moveToFoodSupport);
            CurrentAgent.Commit(ruleMoveToFood);

            SupportCalculator moveToJewelSupport = FixedRuleToMoveToJewel;
            FixedRule ruleMoveToJewel = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, outputMove, moveToJewelSupport);
            CurrentAgent.Commit(ruleMoveToJewel);

            SupportCalculator getJewelSupport = FixedRuleToGetJewel;
            FixedRule ruleGetJewel = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, outputGet, getJewelSupport);
            CurrentAgent.Commit(ruleGetJewel);

            SupportCalculator deliverLeafletSupport = FixedRuleToDeliver;
            FixedRule ruleDeliverLeaflet = AgentInitializer.InitializeActionRule(CurrentAgent, FixedRule.Factory, outputDeliver, deliverLeafletSupport);
            CurrentAgent.Commit(ruleDeliverLeaflet);

            // Disable Rule Refinement
            CurrentAgent.ACS.Parameters.PERFORM_RER_REFINEMENT = false;

            // The selection type will be probabilistic
            CurrentAgent.ACS.Parameters.LEVEL_SELECTION_METHOD = ActionCenteredSubsystem.LevelSelectionMethods.STOCHASTIC;

            // The action selection will be fixed (not variable) i.e. only the statement defined above.
            CurrentAgent.ACS.Parameters.LEVEL_SELECTION_OPTION = ActionCenteredSubsystem.LevelSelectionOptions.FIXED;

            // Define Probabilistic values
            CurrentAgent.ACS.Parameters.FIXED_FR_LEVEL_SELECTION_MEASURE = 1;
            CurrentAgent.ACS.Parameters.FIXED_IRL_LEVEL_SELECTION_MEASURE = 0;
            CurrentAgent.ACS.Parameters.FIXED_BL_LEVEL_SELECTION_MEASURE = 0;
            CurrentAgent.ACS.Parameters.FIXED_RER_LEVEL_SELECTION_MEASURE = 0;
        }

        /// <summary>
        /// Make the agent perception. In other words, translate the information that came from sensors to a new type that the agent can understand
        /// </summary>
        /// <param name="sensorialInformation">The information that came from server</param>
        /// <returns>The perceived information</returns>
		private SensoryInformation prepareSensoryInformation(IList<Thing> listOfThings)
        {
            // New sensory information
            SensoryInformation si = World.NewSensoryInformation(CurrentAgent);

            // Detect if we have a wall ahead
            Boolean wallAhead = listOfThings.Where(item => (item.CategoryId == Thing.CATEGORY_BRICK && item.DistanceToCreature <= 61)).Any();
            double wallAheadActivationValue = wallAhead ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;
            si.Add(inputWallAhead, wallAheadActivationValue);
			Creature c = (Creature) listOfThings.Where(item => (item.CategoryId == Thing.CATEGORY_CREATURE)).First();
			int n = 0;

            foreach (Leaflet l in c.getLeaflets())
            {
                mind.updateLeaflet(n, l);
                n++;
            }

            // Verifica se algum pode ser completado
            bool canComplete = c.getLeaflets().Any(l => l.IsComplete());
            double inputCanCompleteLeafletValue = canComplete ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;
            si.Add(inputCanCompleteLeaflet, inputCanCompleteLeafletValue);

            // Atualiza o Deliver com o leaflet completo (se existir)
            Leaflet leafletToDeliver = c.getLeaflets().FirstOrDefault(l => l.IsComplete());
            deliver.UpdateCompleteLeaflet(leafletToDeliver);

            Console.WriteLine("CANCOMPLETE " + canComplete);
            Console.WriteLine("deliver.leaflet " + deliver.GetLeaflet());

            // SI FOOD
            bool foodAhead = listOfThings.Any(item =>
                        (item.CategoryId == Thing.CATEGORY_FOOD ||
                        item.CategoryId == Thing.categoryPFOOD ||
                        item.CategoryId == Thing.CATEGORY_NPFOOD) &&
                        item.DistanceToCreature <= 20);
            double foodAheadActivationValue = foodAhead ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;
            si.Add(inputFoodAhead, foodAheadActivationValue);

            // SI JEWEL
            double jewelInMemoryActivationValue = memory.GetJewels().Any() ? CurrentAgent.Parameters.MAX_ACTIVATION : CurrentAgent.Parameters.MIN_ACTIVATION;
            si.Add(inputHasJewelInMemory, jewelInMemoryActivationValue);
            bool jewelAhead = listOfThings.Any(item =>
                item.CategoryId == Thing.CATEGORY_JEWEL &&
                item.DistanceToCreature <= 20);
            if (jewelAhead)
            {
                // Ativa o input apenas se a joia estiver próxima E não coletada
                double jewelAheadActivationValue = CurrentAgent.Parameters.MAX_ACTIVATION;
                si.Add(inputJewelAhead, jewelAheadActivationValue);
            }
            else
            {
                // Se não houver joia válida à frente, desativa o input
                si.Add(inputJewelAhead, CurrentAgent.Parameters.MIN_ACTIVATION);
            }

            return si;
        }
        #endregion

        #region Fixed Rules
        private double FixedRuleToAvoidCollisionWall(ActivationCollection currentInput, Rule target)
        {
            bool otherRulesActive =
                    currentInput.Contains(inputCanCompleteLeaflet, CurrentAgent.Parameters.MAX_ACTIVATION) ||
                    currentInput.Contains(inputHasJewelInMemory, CurrentAgent.Parameters.MAX_ACTIVATION) ||
                    currentInput.Contains(inputFoodAhead, CurrentAgent.Parameters.MAX_ACTIVATION) ||
                    currentInput.Contains(inputJewelAhead, CurrentAgent.Parameters.MAX_ACTIVATION) ||
                    currentInput.Contains(inputCanCompleteLeaflet, CurrentAgent.Parameters.MAX_ACTIVATION);
            // Here we will make the logic to go ahead
            return (!otherRulesActive &&
                       currentInput.Contains(inputWallAhead, CurrentAgent.Parameters.MAX_ACTIVATION)) ? 1 : 0.0;
        }

        private double FixedRuleToGoAhead(ActivationCollection currentInput, Rule target)
        {
            bool otherRulesActive =
                    currentInput.Contains(inputCanCompleteLeaflet, CurrentAgent.Parameters.MAX_ACTIVATION) ||
                    currentInput.Contains(inputHasJewelInMemory, CurrentAgent.Parameters.MAX_ACTIVATION) ||
                    currentInput.Contains(inputFoodAhead, CurrentAgent.Parameters.MAX_ACTIVATION) ||
                    currentInput.Contains(inputJewelAhead, CurrentAgent.Parameters.MAX_ACTIVATION) ||
                    currentInput.Contains(inputCanCompleteLeaflet, CurrentAgent.Parameters.MAX_ACTIVATION);
            // Here we will make the logic to go ahead
            return (!otherRulesActive &&
                       currentInput.Contains(inputWallAhead, CurrentAgent.Parameters.MIN_ACTIVATION)) ? 1 : 0.0;
        }
        private double FixedRuleToMoveToFood(ActivationCollection currentInput, Rule target)
        {
            return (
                currentInput.Contains(inputHasFoodInMemory, CurrentAgent.Parameters.MAX_ACTIVATION) &&
                currentInput.Contains(inputLowFuel, CurrentAgent.Parameters.MAX_ACTIVATION) &&
                !currentInput.Contains(inputFoodAhead, CurrentAgent.Parameters.MAX_ACTIVATION) &&
                !currentInput.Contains(inputJewelAhead, CurrentAgent.Parameters.MAX_ACTIVATION)
            ) ? 1.0 : 0.0;
        }

        private double FixedRuleToMoveToJewel(ActivationCollection currentInput, Rule target)
        {
            return (currentInput.Contains(inputHasJewelInMemory, CurrentAgent.Parameters.MAX_ACTIVATION) &&
                !currentInput.Contains(inputFoodAhead, CurrentAgent.Parameters.MAX_ACTIVATION) &&
                !currentInput.Contains(inputJewelAhead, CurrentAgent.Parameters.MAX_ACTIVATION)) ? 1.0 : 0.0;
        }
        private double FixedRuleToGetJewel(ActivationCollection currentInput, Rule target)
        {
            return (currentInput.Contains(inputJewelAhead, CurrentAgent.Parameters.MAX_ACTIVATION)) ? 1.0 : 0.0;
        }
        private double FixedRuleToDeliver(ActivationCollection currentInput, Rule target)
        {
            return (currentInput.Contains(inputCanCompleteLeaflet, CurrentAgent.Parameters.MAX_ACTIVATION)) ? 1.0 : 0.0;
        }
        #endregion

        #region Run Thread Method
        private void CognitiveCycle(object obj)
        {

			Console.WriteLine("Starting Cognitive Cycle ... press CTRL-C to finish !");
            // Cognitive Cycle starts here getting sensorial information
            while (CurrentCognitiveCycle != MaxNumberOfCognitiveCycles)
            {
                // Get current sensory information                    
                IList<Thing> currentSceneInWS3D = processSensoryInformation();

                this.memory.UpdateJewelAndFoodList(currentSceneInWS3D);

                // Make the perception
                SensoryInformation si = prepareSensoryInformation(currentSceneInWS3D);

                //Perceive the sensory information
                CurrentAgent.Perceive(si);

                //Choose an action
                ExternalActionChunk chosen = CurrentAgent.GetChosenExternalAction(si);

                // Get the selected action
                String actionLabel = chosen.LabelAsIComparable.ToString();
                CreatureActions actionType = (CreatureActions)Enum.Parse(typeof(CreatureActions), actionLabel, true);
                Console.WriteLine("finish selection - action " + actionLabel);

                // Call the output event handler
                processSelectedAction(actionType);

                // Increment the number of cognitive cycles
                CurrentCognitiveCycle++;

                //Wait to the agent accomplish his job
                if (TimeBetweenCognitiveCycles > 0)
                {
                    Thread.Sleep(TimeBetweenCognitiveCycles);
                }
                if (Math.Abs(CurrentCognitiveCycle % 1000) < 0.00001)
                {
                    SpawnThings();
                }

            }
        }

        /// <summary>
        /// Thread que cria joias continuamente.
        /// </summary>
        private void SpawnThings()
        {
            // Limites do mapa (ajuste conforme seu cenário)
            int minX = 60;
            int maxX = 500;
            int minY = 60;
            int maxY = 500;


            int jewelType = random.Next(0, 6);
            int foodType = random.Next(0, 1);

            try
            {
                int x = random.Next(minX, maxX);
                int y = random.Next(minY, maxY);
                worldServer.NewJewel(jewelType, x, y);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine($"[ERROR] Failed to create jewel: {e.Message}");
            }

            try
            {
                int x = random.Next(minX, maxX);
                int y = random.Next(minY, maxY);
                worldServer.NewFood(foodType, x, y);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine($"[ERROR] Failed to create food): {e.Message}");
            }

        }
    #endregion
    }
}
