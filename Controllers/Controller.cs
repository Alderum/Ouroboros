using VBTBotConsole3.Indicators;

namespace VBTBotConsole3.Controllers
{
    class Controller
    {
        #region Fields and properties

        string publicKey;
        public string PublicKey {
            get
            {
                return publicKey;
            }
        }

        string secretKey;
        public string SecretKey { 
            get 
            {
                return secretKey;
            }
        }

        TradeController trader;
        public TradeController TradeController {
            get
            {
                return trader;
            }
        }

        ModelController modelController;
        public ModelController ModelController {
            get
            {
                return modelController;
            }
        }

        #endregion

        public Controller()
        {
            modelController = new ModelController();
        }

        public Controller(string publicKey, string secretKey) : this()
        {
            this.publicKey = publicKey;
            this.secretKey = secretKey;
            trader = new TradeController(publicKey, secretKey, this);
        }

        #region Methods

        public List<MovingAvarage> GetEMAOfAllCandles(int depth)
        {
            var klines = ModelController.Klines;
            List<MovingAvarage> ema = MovingAvarage.GetEMA(klines, (decimal)depth);

            return ema;
        }

        #endregion
    }
}
