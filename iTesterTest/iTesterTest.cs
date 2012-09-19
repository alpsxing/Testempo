using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iTestBase;

namespace iTesterTest
{
	public class iTesterTest : iTestBase.iTestBase
	{
        public enum TestObjectiveEnum
        {
            SendPass,
            SendFail,
            SendError,
            SendInfo,
            SendNone
        }

        private TestObjectiveEnum _testObjective = TestObjectiveEnum.SendNone;
        public TestObjectiveEnum TestObjective
        {
            get
            {
                return _testObjective;
            }
            set
            {
                _testObjective = value;
            }
        }

		public iTesterTest()
			: base()
		{
		}

        public override void Setup()
        {
            //base.Setup();
        }

        public override void Clean()
        {
            //base.Clean();
        }

        public override void Run()
        {
            //base.Run();

            switch (TestObjective)
            {
                default:
                case TestObjectiveEnum.SendNone:
                    DoSendMessageOnly(message:"No message.");
                    break;
                case TestObjectiveEnum.SendInfo:
                    DoSendMessageOnly(TestStateEnum.Infromation, "Infromation message.");
                    break;
                case TestObjectiveEnum.SendPass:
                    DoSendMessageOnly(TestStateEnum.Pass, "Pass message.");
                    DoSendMessageOnly(TestStateEnum.Pass, "Pass message.");
                    break;
                case TestObjectiveEnum.SendFail:
                    DoSendMessageOnly(TestStateEnum.Fail, "Fail message.");
                    break;
                case TestObjectiveEnum.SendError:
                    DoSendMessageOnly(TestStateEnum.Error, "Error message.");
                    DoSendMessageOnly(TestStateEnum.Error, "Error message.");
                    break;
            }
        }
	}
}
