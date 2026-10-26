using NUnit.Framework;

namespace Mycelia.Game.Tests
{
    // Design §8.3 slice 8: three pages; Next turns forward and finishes on the last page, back turns back and leaves
    // from the first.
    public class IntroPagesTests
    {
        [Test]
        public void TheIntro_OpensOnItsFirstPage()
        {
            var pages = new IntroPages(3);

            Assert.That(pages.Current, Is.EqualTo(0));
            Assert.That(pages.IsLast, Is.False);
        }

        [Test]
        public void Next_TurnsToTheFollowingPage_AndOnTheLastPageFinishes()
        {
            var pages = new IntroPages(3);

            Assert.That(pages.Next(), Is.EqualTo(IntroStep.Turned));
            Assert.That(pages.Current, Is.EqualTo(1));
            Assert.That(pages.Next(), Is.EqualTo(IntroStep.Turned));
            Assert.That(pages.Current, Is.EqualTo(2));
            Assert.That(pages.IsLast, Is.True);

            Assert.That(pages.Next(), Is.EqualTo(IntroStep.Finished));
            Assert.That(pages.Current, Is.EqualTo(2), "Finishing stays on the last page while the screen changes.");
        }

        [Test]
        public void Back_TurnsToThePreviousPage_AndOnTheFirstPageLeaves()
        {
            var pages = new IntroPages(3);
            pages.Next();
            pages.Next();

            Assert.That(pages.Back(), Is.EqualTo(IntroStep.Turned));
            Assert.That(pages.Current, Is.EqualTo(1));
            Assert.That(pages.Back(), Is.EqualTo(IntroStep.Turned));
            Assert.That(pages.Current, Is.EqualTo(0));

            Assert.That(pages.Back(), Is.EqualTo(IntroStep.Left));
            Assert.That(pages.Current, Is.EqualTo(0));
        }

        [Test]
        public void AnIntroWithoutPages_IsASetupMistake()
        {
            Assert.That(() => new IntroPages(0), Throws.ArgumentException);
        }
    }
}
