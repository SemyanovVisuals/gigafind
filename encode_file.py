
with open("send_file", "wb") as w_file:
    seq_length = 2

    for i in range(seq_length):
        r_file = open(f"seq/{i}.jpg", "rb")
        w_file.write(r_file.read())
        r_file.close()
