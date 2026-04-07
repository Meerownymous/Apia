using Apia.Ram.Core;
using Apia.Ram.Tests.Examples.Record;
using Apia.Tests.Record;

namespace Apia.Ram.Tests.Examples.UseCases.Userfeed;


public sealed class UserFeedSynopsis() : RamSynopsisStreamTmp<UserPostSummary, UserRecord>(Query)
{
    private static async IAsyncEnumerable<UserPostSummary> Query(IMemoryTmp memory, IQuery<UserRecord> query)
    {
        var posts = memory.Entities<PostRecord>();
        var comments = memory.Entities<CommentRecord>();
        var users = memory.Entities<UserRecord>();

        var match = await users.FindSingle(query);

        if (match.IsT0)
        {
            var user =  match.AsT0;
            var feed =
                (await posts.Find(new Query<PostRecord>().Where(p => p.AuthorId).Is(user.UserId)).ToArrayAsync())
                .OrderByDescending(p => p.CreatedAt);

            foreach (var post in feed)
            {
                var commentCount =
                    await
                        comments.Find(new Query<CommentRecord>().Where(c => c.PostId).Is(post.PostId))
                            .CountAsync();

                yield return new UserPostSummary(
                    PostId:       post.PostId,
                    AuthorName:   user.Username,
                    Content:      post.Content,
                    LikeCount:    post.LikeCount,
                    CommentCount: commentCount,
                    CreatedAt:    post.CreatedAt
                );
            }
        }

        if (match.IsT1) throw new ArgumentException("User not found");
        if (match.IsT2)
            throw new ArgumentException($"Ambiguous user query, {match.AsT2.Candidates.Count} candidates found.");
    }
}
