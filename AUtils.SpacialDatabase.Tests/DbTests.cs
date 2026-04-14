using System;
using System.Linq;
using AUtils.Math;
using Xunit;
using Db = AUtils.SpacialDatabase;

namespace AUtils.SpacialDatabase.Tests;

public class DbTests
{
    private SpacialDatabase<Guid> Setup(int cellSize = 100)
    {
        var db = new SpacialDatabase<Guid>(cellSize);
        for (int i = 0; i < 1000; i+=5)
        {
            for (int j = 0; j < 1000; j += 10)
                db.AddOrUpdate(Guid.NewGuid(), new Point3 {X = i * 5, Y = j * 5, Z = 0});
        }

        return db;
    }

    [Fact]
    public void AddOrUpdateTest()
    {
        var db = new SpacialDatabase<Guid>(1000);
        db.AddOrUpdate(Guid.NewGuid(), new Point3 {X = 150, Y = 350, Z = 0});
        var item = Guid.NewGuid();
        db.AddOrUpdate(item, new Point3 {X = 3150, Y = 1350, Z = 0});
        db.AddOrUpdate(item, new Point3 {X = 1150, Y = 1350, Z = 0});

        Assert.True(db.Count() == 3);
        Assert.True(db.SelectMany(x => x.Items).Count() == 2);
        Assert.True(db[0,0].Items.Count == 1);
        Assert.True(db[1,1].Items.Count == 1);
        Assert.True(!db[3,1].Items.Any());
    }
    
    [Fact]
    public void GetByRectTest()
    {
        var db = Setup();
        var items = db.GetByRect(new Point3(400, 400, 0), 200, 200).ToArray();

        Assert.Equal(153, items.Length);
        
        db = Setup(1000);
        items = db.GetByRect(new Point3(400, 400, 0), 200, 200).ToArray();

        Assert.Equal(153, items.Length);
        
        foreach (var item in items)
        {
            var position = db.Get(item.Key);
            Assert.Equal(position, item.Value);
            Assert.True(position.HasValue);
            Assert.True(position.Value.X is >= 200 and <= 600);
            Assert.True(position.Value.Y is >= 200 and <= 600);
        }
    }
}