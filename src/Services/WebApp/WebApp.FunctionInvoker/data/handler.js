function handler(request) {
    while (true) {
        console.log('spam')
    }
    return {
        statusCode: 200,
        headers: {
            "content-type": "application/json"
        },
        body: JSON.stringify({ message: 'Hello from the function invoker!'})
    }
}