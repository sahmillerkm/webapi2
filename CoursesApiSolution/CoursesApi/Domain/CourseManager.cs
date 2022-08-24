using MongoDB.Bson;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace CoursesApi.Domain;

public class CourseManager
{

    private readonly MongoDbCoursesAdapter _adapter;
    private readonly FilterDefinition<CourseEntity> _filterAllCourses;
    private readonly ProjectionDefinition<CourseEntity, CourseDetailsResponse> _projectToCourseDetails;
    private readonly ProjectionDefinition<CourseEntity, CourseSummaryItemResponse> _projectToCourseSummary;
    private readonly Expression<Func<CourseEntity, CourseDetailsResponse>> _mapCourseToDetailsExpression;

    private readonly Func<CourseEntity, CourseDetailsResponse> _mapCourseToDetailsFunc;
    public CourseManager(MongoDbCoursesAdapter adapter)
    {
        _adapter = adapter;
        _filterAllCourses = Builders<CourseEntity>.Filter.Where(c => c.IsRemoved == false);
        _mapCourseToDetailsExpression = (CourseEntity c) => new CourseDetailsResponse
        {
            Id = c.Id.ToString(),
            Title = c.Title,
            DeliveryLocation = c.DeliveryLocation,
            NumberOfHours = c.NumberOfHours
        };
        _projectToCourseDetails = Builders<CourseEntity>.Projection.Expression(_mapCourseToDetailsExpression);

        _projectToCourseSummary = Builders<CourseEntity>.Projection.Expression(c => new CourseSummaryItemResponse
        {
            Id = c.Id.ToString(),
            Title = c.Title
        });

        _mapCourseToDetailsFunc = _mapCourseToDetailsExpression.Compile();
    }

    public async Task<CoursesResponse> GetAllCoursesAsync()
    {
       
        var response = new CoursesResponse
        {
            Data = await _adapter.Courses.Find(_filterAllCourses).Project(_projectToCourseSummary).ToListAsync()
        };

        return response;
    }

    public async Task<CourseDetailsResponse> AddCourseAsync(CourseCreateRequest request)
    {
        // From A Model -> CourseEntity
        var courseToAdd = new CourseEntity
        {
            Title = request.Title,
            NumberOfHours = request.NumberOfHours,
            DeliveryLocation = request.DeliveryLocation,
            IsRemoved = false,
            WhenCreated = DateTime.Now
        };

        await _adapter.Courses.InsertOneAsync(courseToAdd);
  
        return _mapCourseToDetailsFunc(courseToAdd);
    }

    public async Task<CourseDetailsResponse?> GetCourseByIdAsync(ObjectId courseId)
    {
            var byIdFilter = Builders<CourseEntity>.Filter.Where(c => c.Id == courseId);
            var filterByNotRemovedAndById = _filterAllCourses & byIdFilter;

            return await _adapter.Courses
                .Find(filterByNotRemovedAndById)
                .Project(_projectToCourseDetails)
                .SingleOrDefaultAsync();
       
    }
}
