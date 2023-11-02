using Azure.Storage.Blobs.Models;
using Rygg.Runes.Spreads;
using System.Runtime.CompilerServices;
using RyggRunes.Client.Core;
using Microsoft.Extensions.Configuration;
using Rygg.Runes.Data.Core;

namespace Rygg.Runes.Test
{
    [TestClass]
    public class SpreadTests
    {
        protected IConfiguration Configuration { get; set; } = null!;
        [TestInitialize]
        public void Init()
        {
            var builder = new ConfigurationBuilder().AddUserSecrets<SpreadTests>();
            Configuration = builder.Build();
        }
        [TestMethod]
        public async Task ValidAstrologicalSpread()
        {
           StorageBlob blob = new StorageBlob(Configuration);
            var reading = await blob.GetImage("2023-10-28-08:20:17lind@yahoo.com1a2e6fad-845f-4fdf-bc0d-aff8910d1874");
            Assert.IsNotNull(reading);
            var runes = reading.annotations.Select(r => new PlacedRune(r)).ToArray();
            AstrologicalSpread spread = new AstrologicalSpread();
            PlacedRune?[,] matrix;
            var sr = spread.Validate(runes, out matrix);


        }
        [TestMethod]
        public async Task ValidChoiceSpread()
        {
            StorageBlob blob = new StorageBlob(Configuration);
            var reading = await blob.GetImage("2023-10-29-01:44:08lind@yahoo.com7d574359-ab59-4d7a-b771-b7a38e5de3ea");
            Assert.IsNotNull(reading);
            var runes = reading.annotations.Select(r => new PlacedRune(r)).ToArray();
            ChoiceSpread spread = new ChoiceSpread();
            PlacedRune?[,] matrix;
            var sr = spread.Validate(runes, out matrix);
        }
        [TestMethod]
        public async Task ValidSimpleLoveSpread()
        {
            StorageBlob blob = new StorageBlob(Configuration);
            var reading = await blob.GetImage("2023-10-29-02:21:41lind@yahoo.com28e1cb23-01ea-429a-bde2-c54c580d1db5");
            Assert.IsNotNull(reading);
            var runes = reading.annotations.Select(r => new PlacedRune(r)).ToArray();
            SimpleLoveSpread spread = new SimpleLoveSpread();
            PlacedRune?[,] matrix;
            var sr = spread.Validate(runes, out matrix);


        }
        [TestMethod]
        public async Task ValidCurrentRelationshipSpread()
        {
            StorageBlob blob = new StorageBlob(Configuration);
            var reading = await blob.GetImage("2023-10-29-02:31:21lind@yahoo.com4867e8bd-17c2-4a83-bb37-7a986efcf9b0");
            Assert.IsNotNull(reading);
            var runes = reading.annotations.Select(r => new PlacedRune(r)).ToArray();
            CurrentRelationshipSpread spread = new CurrentRelationshipSpread();
            PlacedRune?[,] matrix;
            var sr = spread.Validate(runes, out matrix);


        }
        [TestMethod]
        public async Task ValidCelticCrossSpread()
        {
            StorageBlob blob = new StorageBlob(Configuration);
            var reading = await blob.GetImage("2023-10-29-02:52:39lind@yahoo.come07af7cb-3d29-4d13-9f52-2d4f1c8d74a6");
            Assert.IsNotNull(reading);
            var runes = reading.annotations.Select(r => new PlacedRune(r)).ToArray();
            CelticCrossSpread spread = new CelticCrossSpread();
            PlacedRune?[,] matrix;
            var sr = spread.Validate(runes, out matrix);


        }
        [TestMethod]
        public async Task ValidAnswerToWhySpread()
        {
            StorageBlob blob = new StorageBlob(Configuration);
            var reading = await blob.GetImage("2023-10-29-03:16:36lind@yahoo.com88c45802-1209-4d9a-a83c-433d062ce489");
            Assert.IsNotNull(reading);
            var runes = reading.annotations.Select(r => new PlacedRune(r)).ToArray();
            AnswerToWhySpread spread = new AnswerToWhySpread();
            PlacedRune?[,] matrix;
            var sr = spread.Validate(runes, out matrix);


        }
        [TestMethod]
        public async Task ValidNornsSpread()
        {
            StorageBlob blob = new StorageBlob(Configuration);
            var reading = await blob.GetImage("2023-10-29-03:37:51lind@yahoo.com497bd007-8f99-4ede-95e7-908f30411dea");
            Assert.IsNotNull(reading);
            var runes = reading.annotations.Select(r => new PlacedRune(r)).ToArray();
            NornsSpread spread = new NornsSpread();
            PlacedRune?[,] matrix;
            var sr = spread.Validate(runes, out matrix);


        }
        [TestMethod]
        public async Task ValidSevenGemsSpread()
        {
            StorageBlob blob = new StorageBlob(Configuration);
            var reading = await blob.GetImage("2023-10-29-04:39:23lind@yahoo.comdff1e04d-c3b8-4f62-9fe1-31ccefd524fc");
            Assert.IsNotNull(reading);
            var runes = reading.annotations.Select(r => new PlacedRune(r)).ToArray();
            SevenGemsSpread spread = new SevenGemsSpread();
            PlacedRune?[,] matrix;
            var sr = spread.Validate(runes, out matrix);


        }
    }
}