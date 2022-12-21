﻿namespace PusherClient
{
    class Constants
    {
        public const string PUSHER_MESSAGE_PREFIX = "pusher";
        public const string ERROR = "pusher:error";

        public const string CONNECTION_ESTABLISHED = "pusher:connection_established";
        public const string PUSHER_SIGNIN_SUCCESS = "pusher:signin_success";
        public const string PUSHER_SIGNIN = "pusher:signin";


        public const string CHANNEL_SUBSCRIBE = "pusher:subscribe";
        public const string CHANNEL_UNSUBSCRIBE = "pusher:unsubscribe";
        public const string CHANNEL_SUBSCRIPTION_SUCCEEDED = "pusher_internal:subscription_succeeded";
        public const string CHANNEL_SUBSCRIPTION_COUNT = "pusher_internal:subscription_count";
        public const string CHANNEL_SUBSCRIPTION_ERROR = "pusher_internal:subscription_error";
        public const string CHANNEL_MEMBER_ADDED = "pusher_internal:member_added";
        public const string CHANNEL_MEMBER_REMOVED = "pusher_internal:member_removed";
        public const string WATCHLIST_ONLINE_STATUS = "pusher_internal:watchlist_events";


        public const string INSECURE_SCHEMA = "ws://";
        public const string SECURE_SCHEMA = "wss://";

        public const string PRIVATE_CHANNEL = "private-";
        public const string PRIVATE_ENCRYPTED_CHANNEL = "private-encrypted-";
        public const string PRESENCE_CHANNEL = "presence-";
    }
}