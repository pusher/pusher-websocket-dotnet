public class SigninMessage {

    private String event = Constants.PUSHER_SIGNIN;
    private Dictionary<String, String> data = new Dictionary<String, String>();

    public SigninMessage(String auth, String userData) {
        data.Add("auth", auth);
        data.Add("user_data", userData);
    }
}