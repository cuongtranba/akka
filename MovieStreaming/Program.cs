using System;
using Akka.Actor;
using Akka.Actor.Internal;
using MovieStreaming.Actors;
using MovieStreaming.Messages;

namespace MovieStreaming
{
    class Program
    {
        private static ActorSystem _movieStreamingActorSystem;

        static void Main(string[] args)
        {
            _movieStreamingActorSystem = ActorSystem.Create("MovieStreamingActorSystem");
            Console.WriteLine("Actor system created");

            var userActorProps = Props.Create<UserActor>();
            var playbackActorRef = _movieStreamingActorSystem.ActorOf(userActorProps, "PlayBackActor");

            Console.ReadLine();
            Console.WriteLine("sending a playmovemessage 1");
            playbackActorRef.Tell(new PlayMovieMessage("message 1",1));

            Console.ReadLine();
            Console.WriteLine("sending a playmovemessage 2");
            playbackActorRef.Tell(new PlayMovieMessage("message 2",2));

            Console.ReadLine();
            Console.WriteLine("sending a stopmessage 1");
            playbackActorRef.Tell(new StopMovieMessage());

            Console.ReadLine();
            Console.WriteLine("sending a stopmessage 2");
            playbackActorRef.Tell(new StopMovieMessage());

            Console.ReadLine();
        }
    }
}
