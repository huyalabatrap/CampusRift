using CampusRift.Learning;

namespace CampusRift.Progression
{
    // Lets the learning engine keep its lesson progress inside the V2 profile instead of a file of its own.
    public sealed class ProfileLearningStore : ILearningStore
    {
        readonly ProfileService profile;
        public ProfileLearningStore(ProfileService profile) { this.profile = profile; }
        public LearningProgress Load() => profile.Data.learning;
        public void Save(LearningProgress data) { profile.Data.learning = data; profile.MarkDirty(); }
    }
}
