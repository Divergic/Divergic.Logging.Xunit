﻿namespace Divergic.Logging.Xunit.UnitTests
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;
    using FluentAssertions;
    using global::Xunit;
    using global::Xunit.Abstractions;
    using Microsoft.Extensions.Logging;
    using ModelBuilder;
    using NSubstitute;

    public class TestOutputLoggerTests
    {
        private readonly ITestOutputHelper _output;

        public TestOutputLoggerTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void BeginScopeReturnsInstance()
        {
            var name = Guid.NewGuid().ToString();
            var state = Guid.NewGuid().ToString();

            var sut = new TestOutputLogger(name, _output);

            var actual = sut.BeginScope(state);

            actual.Should().NotBeNull();

            Action action = () => actual.Dispose();

            action.Should().NotThrow();
        }

        [Fact]
        public void BeginScopeShouldNotThrowWhenStateDataHasCircularReference()
        {
            var name = Guid.NewGuid().ToString();
            var state = new CircularReference();

            var sut = new TestOutputLogger(name, _output);

            using (sut.BeginScope(state))
            {
                sut.LogDebug("...");
            }
        }

        [Theory]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.None)]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Warning)]
        public void IsEnabledReturnsTrueTest(LogLevel logLevel)
        {
            var name = Guid.NewGuid().ToString();

            var sut = new TestOutputLogger(name, _output);

            var actual = sut.IsEnabled(logLevel);

            actual.Should().BeTrue();
        }

        [Fact]
        public void LogIgnoresTestBoundaryFailure()
        {
            // This test should not fail the test runner
            var name = Guid.NewGuid().ToString();
            var config = new LoggingConfig {IgnoreTestBoundaryException = true};

            var sut = new TestOutputLogger(name, _output, config);

            var task = new Task(async () =>
            {
                await Task.Delay(0).ConfigureAwait(false);

                sut.LogCritical("message2");
            });

            task.Start();
        }

        [Fact]
        public void LogUsesDefaultFormatterWhenConfigFormatterIsNull()
        {
            var logLevel = LogLevel.Error;
            var eventId = Model.Create<EventId>();
            var state = Guid.NewGuid().ToString();
            var message = Guid.NewGuid().ToString();
            Func<string, Exception, string> formatter = (logState, error) => message;
            var name = Guid.NewGuid().ToString();
            var config = new LoggingConfig {Formatter = null};

            var expected = string.Format(CultureInfo.InvariantCulture,
                "{0}{1} [{2}]: {3}\r\n",
                string.Empty,
                logLevel,
                eventId.Id,
                message);

            var output = Substitute.For<ITestOutputHelper>();

            var sut = new TestOutputLogger(name, output, config);

            sut.Log(logLevel, eventId, state, null, formatter);

            output.Received(1).WriteLine(Arg.Any<string>());
            output.Received().WriteLine(expected);
        }

        [Fact]
        public void LogUsesDefaultFormatterWhenConfigIsNull()
        {
            var logLevel = LogLevel.Error;
            var eventId = Model.Create<EventId>();
            var state = Guid.NewGuid().ToString();
            var message = Guid.NewGuid().ToString();
            Func<string, Exception, string> formatter = (logState, error) => message;
            var name = Guid.NewGuid().ToString();
            var expected = string.Format(CultureInfo.InvariantCulture,
                "{0}{1} [{2}]: {3}\r\n",
                string.Empty,
                logLevel,
                eventId.Id,
                message);

            var output = Substitute.For<ITestOutputHelper>();

            var sut = new TestOutputLogger(name, output);

            sut.Log(logLevel, eventId, state, null, formatter);

            output.Received(1).WriteLine(Arg.Any<string>());
            output.Received().WriteLine(expected);
        }

        [Fact]
        public void LogWritesExceptionTest()
        {
            var logLevel = LogLevel.Information;
            var eventId = Model.Create<EventId>();
            var state = Guid.NewGuid().ToString();
            var message = Guid.NewGuid().ToString();
            var exception = new ArgumentNullException(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            Func<string, Exception, string> formatter = (logState, error) => message;
            var name = Guid.NewGuid().ToString();

            var output = Substitute.For<ITestOutputHelper>();

            var sut = new TestOutputLogger(name, output);

            sut.Log(logLevel, eventId, state, exception, formatter);

            output.Received(1).WriteLine(Arg.Any<string>());
            output.Received().WriteLine(Arg.Is<string>(x => x.Contains(exception.ToString())));
        }

        [Theory]
        [InlineData(LogLevel.Critical)]
        [InlineData(LogLevel.Debug)]
        [InlineData(LogLevel.Error)]
        [InlineData(LogLevel.Information)]
        [InlineData(LogLevel.None)]
        [InlineData(LogLevel.Trace)]
        [InlineData(LogLevel.Warning)]
        public void LogWritesLogLevelToOutputTest(LogLevel logLevel)
        {
            var eventId = Model.Create<EventId>();
            var state = Guid.NewGuid().ToString();
            var message = Guid.NewGuid().ToString();
            Func<string, Exception, string> formatter = (logState, error) => message;
            var name = Guid.NewGuid().ToString();
            var expected = string.Format(CultureInfo.InvariantCulture,
                "{0}{1} [{2}]: {3}\r\n",
                string.Empty,
                logLevel,
                eventId.Id,
                message);

            var output = Substitute.For<ITestOutputHelper>();

            var sut = new TestOutputLogger(name, output);

            sut.Log(logLevel, eventId, state, null, formatter);

            output.Received(1).WriteLine(Arg.Any<string>());
            output.Received().WriteLine(expected);
        }

        [Fact]
        public void LogWritesMessageUsingSpecifiedLineFormatter()
        {
            var logLevel = LogLevel.Error;
            var eventId = Model.Create<EventId>();
            var state = Guid.NewGuid().ToString();
            var message = Guid.NewGuid().ToString();
            var exception = new ArgumentNullException(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            var name = Guid.NewGuid().ToString();
            var expected = Guid.NewGuid().ToString();
            Func<string, Exception, string> lineFormatter = (logState, error) => message;

            var formatter = Substitute.For<ILogFormatter>();
            var config = new LoggingConfig {Formatter = formatter};

            formatter.Format(0, name, logLevel, eventId, message, exception).Returns(expected);

            var output = Substitute.For<ITestOutputHelper>();

            var sut = new TestOutputLogger(name, output, config);

            sut.Log(logLevel, eventId, state, exception, lineFormatter);

            formatter.Received().Format(0, name, logLevel, eventId, message, exception);

            output.Received().WriteLine(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        public void ThrowsExceptionWhenCreatedWithInvalidNameTest(string name)
        {
            var output = Substitute.For<ITestOutputHelper>();

            Action action = () => new TestOutputLogger(name, output);

            action.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ThrowsExceptionWhenCreatedWithNullOutput()
        {
            var name = Guid.NewGuid().ToString();

            Action action = () => new TestOutputLogger(name, null);

            action.Should().Throw<ArgumentNullException>();
        }
    }
}