using Akka.Actor;
using Akka.Event;
using Example.Actors;
using Example.Messages;
using System;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var system = ActorSystem.Create("iot-system"))
            {
                var supervior = system.ActorOf(Props.Create<IotSupervisor>(), "iot-supervisor");
                var deviceActor = system.ActorOf(Device.Props("proup", "device"));

                deviceActor.Tell(new RecordTemperature(1,23),supervior);
                deviceActor.Tell(new ReadTemperature(requestId: 2), supervior);

                deviceActor.Tell(new RecordTemperature(requestId: 3, value: 55.0), supervior);
                deviceActor.Tell(new ReadTemperature(requestId: 4), supervior);


                Console.ReadLine();
            }
        }
    }

   
}
