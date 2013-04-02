﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using ExpressionMapper.Mapping;
using ExpressionMapper.Extensions;
using ExpressionMapper.Tests.Example;

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
                IsPredator = true
            };
            return from;
        }

        private static Animal GetTestModel()
        {
            var from = new Animal
            {
                Species = "Tiger",
                Name = "Tigger",
                Weight = 300,
                IsTame = true,
                Id = Guid.NewGuid(),
                Price = 3350.20m,
                IsFlat = true.ToString(),
                Endangered = true,

                Color = "Orange",
                IsPredator = true
            };
            return from;
        }
        #endregion

        #region Assertions
        private static void AssertDtoCorrectlyMappedToModel(AnimalDTO from, Animal to)
        {
            Assert.IsNotNull(from, "DTO (From) should not be null");
            Assert.IsNotNull(to, "Model (To) should not be null");

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
        }

        private static void AssertFromModelCorrectlyMappedToDto(Animal from, AnimalDTO to)
        {
            Assert.IsNotNull(from, "Model (From) should not be null");
            Assert.IsNotNull(to, "DTO (To) should not be null");

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
        } 
        #endregion

        private static readonly ExpressionMapperFactory mapper = new ExpressionMapperFactory();

        [TestMethod]
        public void MapperFactory_Create_Mapper_From_Not_Null()
        {
            var from = GetTestDTO();

            var map = mapper.CreateImplicitlyMappedType<AnimalDTO, Animal>();

            var to = map(from);

            AssertDtoCorrectlyMappedToModel(from, to);
        }

        [TestMethod]
        public void MapperFactory_Create_Mapper_From_Null_Throws_NullRefException()
        {
            AnimalDTO from = null;

            var map = mapper.CreateImplicitlyMappedType<AnimalDTO, Animal>();

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
            
            var map = mapper.CreateImplicitTypeMapper<AnimalDTO, Animal>();

            map(from, to);

            AssertDtoCorrectlyMappedToModel(from, to);
        }

        [TestMethod]
        public void MapperFactory_Map_Model_To_DTO()
        {
            var from = GetTestModel();

            var to = new AnimalDTO { Sound = "Growl" };
            var map = mapper.CreateImplicitTypeMapper<Animal, AnimalDTO>();

            map(from, to);

            AssertFromModelCorrectlyMappedToDto(from, to);
        }

        [TestMethod]
        public void MapperFactory_Map_Null_From_Throws_NullRefException()
        {
            AnimalDTO from = null;

            var to = new Animal { Sound = "Growl" };
            var map = mapper.CreateImplicitTypeMapper<AnimalDTO, Animal>();

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
            var map = mapper.CreateImplicitTypeMapper<AnimalDTO, Animal>();

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
