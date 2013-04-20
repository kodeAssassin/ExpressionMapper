using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ExpressionMapper.Mapping;
using ExpressionMapper.Extensions;
using ExpressionMapper.Tests.Example;
using System.Linq.Expressions;

namespace ExpressionMapperTests
{
    [TestClass]
    public class Tests
    {
        #region Helper Methods
        private static AnimalDTO GetTestDTO()
        {
            var from = new AnimalDTO
            {
                TrackId = Guid.NewGuid(),
                Species = "Tiger",
                Name = "Tigger",
                Weight = 300,
                IsTame = true,
                Id = Guid.NewGuid().ToString(),
                Price = "3350.20",
                IsFlat = true,
                Endangered = 1,
                Code = Guid.NewGuid(),

                Color = "Orange",
                IsPredator = true,
                Created = DateTime.UtcNow.AddYears(-1).ToString(),
                Updated = DateTime.UtcNow.AddMonths(-1),
                HandlerIds = new List<String> { "29164", "1855", "1450"}
            };
            return from;
        }

        private static Animal GetTestModel()
        {
            var from = new Animal
            {
                TrackId = Guid.NewGuid(),
                Species = "Tiger",
                Name = "Tigger",
                Weight = 300,
                IsTame = true,
                Id = Guid.NewGuid(),
                Price = 3350.20m,
                IsFlat = true.ToString(),
                Endangered = true,

                Color = "Orange",
                IsPredator = true,
                Created = DateTime.UtcNow.AddYears(-1),
                Updated = DateTime.UtcNow.AddMonths(-1)
            };
            return from;
        }
        #endregion

        #region Assertions
        private static void Assert_DTO_Correctly_Mapped_To_Model(AnimalDTO from, Animal to)
        {
            Assert.IsNotNull(from, "DTO (From) should not be null");
            Assert.IsNotNull(to, "Model (To) should not be null");

            Assert.AreEqual(from.TrackId, to.TrackId);
            Assert.AreEqual(from.Species, to.Species);
            Assert.AreEqual(from.Name, to.Name);
            Assert.AreEqual(from.Sound, to.Sound);
            Assert.AreEqual(from.Age ?? 0, to.Age);
            Assert.AreEqual(from.Weight, to.Weight);
            Assert.AreEqual(from.IsTame, to.IsTame ?? false);
            Assert.AreEqual(Guid.Parse(from.Id), to.Id);
            Assert.AreEqual(Decimal.Parse(from.Price), to.Price);
            Assert.AreEqual(from.IsFlat.ToString(), to.IsFlat);
            Assert.AreEqual(from.Imported.IsNullOrEmpty() ? false : bool.Parse(from.Imported), to.Imported);
            Assert.AreEqual(from.SpecialDiet ?? false, to.SpecialDiet);
            Assert.AreEqual(from.Endangered.HasValue ? Convert.ToBoolean(from.Endangered) : false, to.Endangered);
            Assert.AreEqual(from.Code.HasValue ? from.Code.ToString() : null, to.Code);

            Assert.AreEqual(from.Color, to.Color);
            Assert.AreEqual(from.IsPredator, to.IsPredator ?? false);

            Assert.AreEqual(from.Created, to.Created.ToString());
            Assert.AreEqual(from.Updated, to.Updated);
            Assert.IsTrue((new Func<AnimalDTO, Animal, bool>((f, t) =>
            {
                Assert.IsNotNull(f.HandlerIds);
                Assert.IsTrue(f.HandlerIds.Count() > 0);

                Assert.IsNotNull(t.HandlerIds);
                Assert.IsTrue(t.HandlerIds.Count() > 0);

                Assert.AreEqual(f.HandlerIds.Count(), t.HandlerIds.Count());

                for (int i = 0; i < f.HandlerIds.Count(); i++)
                {
                    Assert.AreEqual(f.HandlerIds[i], t.HandlerIds[i].ToString());
                }

                return true;
            }))(from, to));
        }

        private static void Assert_From_Model_Correctly_Mapped_To_DTO(Animal from, AnimalDTO to)
        {
            Assert.IsNotNull(from, "Model (From) should not be null");
            Assert.IsNotNull(to, "DTO (To) should not be null");

            Assert.AreEqual(from.TrackId, to.TrackId);
            Assert.AreEqual(from.Species, to.Species);
            Assert.AreEqual(from.Name, to.Name);
            Assert.AreEqual(from.Sound, to.Sound);
            Assert.AreEqual(from.Age, to.Age ?? 0);
            Assert.AreEqual(from.Weight, to.Weight);
            Assert.AreEqual(from.IsTame ?? false, to.IsTame);
            Assert.AreEqual(from.Id, Guid.Parse(to.Id));
            Assert.AreEqual(from.Price, Decimal.Parse(to.Price));
            Assert.AreEqual(from.IsFlat, to.IsFlat.ToString());
            Assert.AreEqual(from.Imported, to.Imported.IsNullOrEmpty() ? false : bool.Parse(to.Imported));
            Assert.AreEqual(from.SpecialDiet, to.SpecialDiet ?? false);
            Assert.AreEqual(from.Endangered, to.Endangered.HasValue ? Convert.ToBoolean(to.Endangered) : false);
            Assert.AreEqual(from.Code, to.Code.HasValue ? to.ToString() : null);

            Assert.AreEqual(from.Color, to.Color);
            Assert.AreEqual(from.IsPredator ?? false, to.IsPredator);

            Assert.AreEqual(from.Created.ToString(), to.Created);
            Assert.AreEqual(from.Updated, to.Updated);
        } 
        #endregion

        private static readonly ExpressionMapperFactory _mapper;

        static Tests()
        {
            _mapper = (new ExpressionMapperFactory()).RegisterCustomConverter(new Func<String, decimal>((num) =>
                                                   {
                                                       if (!String.IsNullOrEmpty(num))
                                                       {
                                                           return decimal.Parse(num);
                                                       }

                                                       return 0;
                                                   }));
        }

        [TestMethod]
        public void MapperFactory_Create_Mapper_From_Not_Null()
        {
            var from = GetTestDTO();

            var map = _mapper.CreateImplicitlyMappedType<AnimalDTO, Animal>();

            var to = map(from);

            Assert_DTO_Correctly_Mapped_To_Model(from, to);
        }

        [TestMethod]
        public void MapperFactory_Create_Mapper_From_Null_Throws_NullRefException()
        {
            AnimalDTO from = null;

            var map = _mapper.CreateImplicitlyMappedType<AnimalDTO, Animal>();

            try
            {
                var to = map(from);

                Assert.Fail("Failed to throw expected Null Ref Exception");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is NullReferenceException, "Expects Null Ref Exception");
            }
        }

        [TestMethod]
        public void MapperFactory_Map_DTO_To_Model()
        {
            var from = GetTestDTO();

            var to = new Animal { Sound = "Growl" };
            
            var map = _mapper.CreateImplicitTypeMapper<AnimalDTO, Animal>();

            map(from, to);

            Assert_DTO_Correctly_Mapped_To_Model(from, to);
        }

        [TestMethod]
        public void MapperFactory_Map_Model_To_DTO()
        {
            var from = GetTestModel();

            var to = new AnimalDTO { Sound = "Growl" };

            var map = _mapper.CreateImplicitTypeMapper<Animal, AnimalDTO>();

            map(from, to);

            Assert_From_Model_Correctly_Mapped_To_DTO(from, to);
        }

        [TestMethod]
        public void MapperFactory_Map_Null_From_Throws_NullRefException()
        {
            var map = _mapper.CreateImplicitTypeMapper<AnimalDTO, Animal>();
            
            AnimalDTO from = null;
            
            var to = new Animal { Sound = "Growl" };

            try
            {
                map(from, to);

                Assert.Fail("Failed to throw expected Null Ref Exception");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is NullReferenceException, "Expects Null Ref Exception");
            }
        }

        [TestMethod]
        public void MapperFactory_Map_Null_To()
        {
            var from = GetTestDTO();

            Animal to = null;
            var map = _mapper.CreateImplicitTypeMapper<AnimalDTO, Animal>();

            try
            {
                map(from, to);

                Assert.Fail("Failed to throw expected Null Ref Exception");
            }
            catch (Exception ex)
            {
                Assert.IsTrue(ex is NullReferenceException, "Expects Null Ref Exception");
            }
        }
    }
}
