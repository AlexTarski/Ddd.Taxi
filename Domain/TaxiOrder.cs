using Ddd.Taxi.Infrastructure;
using System.Diagnostics.Metrics;
using System.Globalization;

namespace Ddd.Taxi.Domain;

// In real aplication it whould be the place where database is used to find driver by its Id.
// But in this exercise it is just a mock to simulate database
public class DriversRepository
{
	public Driver FillDriverToOrder(int driverId)
	{
		if (driverId == 15)
		{
			return new Driver(new PersonName("Drive", "Driverson"), driverId, new Car("Lada sedan", "A123BT 66", "Baklazhan"));
		}
		else
			throw new Exception("Unknown driver id " + driverId);
	}
}

public class TaxiApi : ITaxiApi<TaxiOrder>
{
	private readonly DriversRepository driversRepo;
	private readonly Func<DateTime> currentTime;
	private int idCounter;

	public TaxiApi(DriversRepository driversRepo, Func<DateTime> currentTime)
	{
		this.driversRepo = driversRepo;
		this.currentTime = currentTime;
	}

    public TaxiOrder CreateOrderWithoutDestination(string firstName, string lastName, string street, string building)
    {
        idCounter++;
		return TaxiOrder.CreateOrderWithoutDestination(idCounter - 1, 
			new PersonName(firstName, lastName), 
			new Address(street, building), 
			currentTime);		
    }


    public void UpdateDestination(TaxiOrder order, string street, string building)
	{
		order.UpdateDestination(new Address(street, building));
	}

    public void AssignDriver(TaxiOrder order, int driverId)
    {
		order.AssignDriver(driversRepo.FillDriverToOrder(driverId), currentTime);
    }

    public void UnassignDriver(TaxiOrder order)
    {
		order.UnassignDriver();
    }

    public string GetDriverFullInfo(TaxiOrder order)
    {
		return order.GetDriverFullInfo();
    }

    public string GetShortOrderInfo(TaxiOrder order)
    {
        return order.GetShortOrderInfo();
    }

    public void Cancel(TaxiOrder order)
    {
		order.Cancel(currentTime);
    }

    public void StartRide(TaxiOrder order)
    {
		order.StartRide(currentTime);
    }

    public void FinishRide(TaxiOrder order)
    {
        order.FinishRide(currentTime);
    }
}

public class TaxiOrder : Entity<int>
{
	private readonly int id;
	public PersonName ClientName { get; }
	public Address Start { get; }
    public Address Destination { get; private set; }
    public Driver Driver { get; private set; }
    public TaxiOrderStatus Status { get; private set; }
    public DateTime CreationTime { get; }
    public DateTime DriverAssignmentTime { get; private set; }
    public DateTime CancelTime { get; private set; }
    public DateTime StartRideTime { get; private set; }
    public DateTime FinishRideTime { get; private set; }

    public TaxiOrder(int id) : base(id)
	{
		this.id = id;
	}

    public TaxiOrder(int id, PersonName clientName, Address startAddress, DateTime dateTime) : base(id)
    {
        this.id = id;
        ClientName = clientName;
        Start = startAddress;
        CreationTime = dateTime;
        Destination = new Address(null, null);
        Driver = Driver.CreateUnassignedDriver();
    }

    public static TaxiOrder CreateOrderWithoutDestination(int idCounter, PersonName client, Address start, Func<DateTime> currentTime)
    {
		return new TaxiOrder(idCounter, client, start, currentTime());
    }

    public void UpdateDestination(Address newDestination)
    {
        this.Destination = newDestination;
    }

    public void AssignDriver(Driver driver, Func<DateTime> currentTime)
    {
        if(this.Driver.Name.FirstName != null)
        {
            throw new InvalidOperationException("Driver is already assigned");
        }
        this.Driver = driver;
        this.DriverAssignmentTime = currentTime();
        this.Status = TaxiOrderStatus.WaitingCarArrival;
    }

    public void UnassignDriver()
    {
        if(this.Driver.Name.FirstName == null)
        {
            throw new InvalidOperationException("WaitingForDriver");
        }

        if(this.Status == TaxiOrderStatus.InProgress)
        {
            throw new InvalidOperationException("Cannot unassign driver after begining of execution of the order");
        }
        this.Driver = Driver.CreateUnassignedDriver();
        this.Status = TaxiOrderStatus.WaitingForDriver;
    }

	public string GetDriverFullInfo()
	{
        if (this.Status == TaxiOrderStatus.WaitingForDriver) return null;
        return string.Join(" ",
            "Id: " + this.Driver.ID,
            "DriverName: " + FormatName(this.Driver.Name.FirstName, this.Driver.Name.LastName),
            "Color: " + this.Driver.Car.CarColor,
            "CarModel: " + this.Driver.Car.CarModel,
            "PlateNumber: " + this.Driver.Car.CarPlateNumber);
    }

    public string GetShortOrderInfo()
    {
        return string.Join(" ",
            "OrderId: " + this.Id,
            "Status: " + this.Status,
            "Client: " + FormatName(this.ClientName.FirstName, this.ClientName.LastName),
            "Driver: " + FormatName(this.Driver.Name.FirstName, this.Driver.Name.LastName),
            "From: " + FormatAddress(this.Start.Street, this.Start.Building),
            "To: " + FormatAddress(this.Destination.Street, this.Destination.Building),
            "LastProgressTime: " + GetLastProgressTime(this).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
    }

    public void Cancel(Func<DateTime> currentTime)
    {
        if (this.Status == TaxiOrderStatus.InProgress)
        {
            throw new InvalidOperationException("Cannot cancel order after begining of its execution");
        }

        this.Status = TaxiOrderStatus.Canceled;
        this.CancelTime = currentTime();
    }

    public void StartRide(Func<DateTime> currentTime)
    {
        if (this.Driver.Name.FirstName == null)
        {
            throw new InvalidOperationException("WaitingForDriver");
        }

        this.Status = TaxiOrderStatus.InProgress;
        this.StartRideTime = currentTime();
    }

    public void FinishRide(Func<DateTime> currentTime)
    {
        if (this.Driver.Name.FirstName == null)
        {
            throw new InvalidOperationException("WaitingForDriver");
        }

        if (this.Status != TaxiOrderStatus.InProgress)
        {
            throw new InvalidOperationException("Cannot cancel order after begining of its execution");
        }

        this.Status = TaxiOrderStatus.Finished;
        this.FinishRideTime = currentTime();
    }


    private static string FormatName(string firstName, string lastName)
    {
        return string.Join(" ", new[] { firstName, lastName }.Where(n => n != null));
    }

    private static string FormatAddress(string street, string building)
    {
        return string.Join(" ", new[] { street, building }.Where(n => n != null));
    }

    private static DateTime GetLastProgressTime(TaxiOrder order)
    {
        if (order.Status == TaxiOrderStatus.WaitingForDriver) return order.CreationTime;
        if (order.Status == TaxiOrderStatus.WaitingCarArrival) return order.DriverAssignmentTime;
        if (order.Status == TaxiOrderStatus.InProgress) return order.StartRideTime;
        if (order.Status == TaxiOrderStatus.Finished) return order.FinishRideTime;
        if (order.Status == TaxiOrderStatus.Canceled) return order.CancelTime;
        throw new NotSupportedException(order.Status.ToString());
    }
}

public class Driver : Entity<int>
{
	public PersonName Name;
	public readonly int ID;
	public Car Car;
	
	public Driver (PersonName name, int id, Car car) : base(id)
	{
		this.Name = name;
		this.ID = id;
		this.Car = car;
	}

    public static Driver CreateUnassignedDriver()
    {
        return new Driver(new PersonName(null, null), -1, new Car());
    }
}

public class Car : ValueType<Car>
{
	public string CarColor;
	public string CarModel;
	public string CarPlateNumber;

	public Car(string carModel = null, string carPlateNumber = null, string carColor = null)
    {
        CarColor = carColor;
        CarModel = carModel;
        CarPlateNumber = carPlateNumber;
    }
}